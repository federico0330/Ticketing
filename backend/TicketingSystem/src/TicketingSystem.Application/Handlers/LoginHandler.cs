using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Security;

namespace TicketingSystem.Application.Handlers;

public class LoginHandler : ILoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public LoginHandler(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return null;

        bool isValid = false;
        try
        {
            isValid = PasswordHasher.Verify(request.Password, user.PasswordHash);
        }
        catch
        {
            // Handle potential format errors for old plaintext passwords
        }

        if (!isValid && user.PasswordHash != request.Password)
            return null;

        var adminUsers = _configuration.GetSection("AdminUsers").Get<string[]>() ?? Array.Empty<string>();
        var role = adminUsers.Contains(user.Email) ? "Admin" : "User";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt Key is missing"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponse(user.Id, user.Name, user.Email, tokenString, role);
    }
}