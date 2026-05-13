using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Security;

public class JwtTokenService : IJwtTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, string role)
    {
        var keyValue = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt Key is missing");
        var key = Encoding.UTF8.GetBytes(keyValue);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public string ResolveRole(string email)
    {
        // Roles dinámicos en appsettings: la tabla USER no tiene columna Role (regla de schema),
        // así que el set de admins se externaliza y se resuelve en runtime.
        var adminUsers = _configuration.GetSection("AdminUsers").Get<string[]>() ?? Array.Empty<string>();
        return adminUsers.Contains(email) ? UserRole.Admin : UserRole.User;
    }
}
