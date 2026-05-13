using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Security;

namespace TicketingSystem.Application.Handlers;

public class LoginHandler : ILoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse?> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return null;

        if (!IsPasswordValid(request.Password, user.PasswordHash))
            return null;

        var role = _jwtTokenService.ResolveRole(user.Email);
        var token = _jwtTokenService.GenerateToken(user, role);

        return new LoginResponse(user.Id, user.Name, user.Email, token, role);
    }

    private static bool IsPasswordValid(string password, string storedHash)
    {
        try
        {
            if (PasswordHasher.Verify(password, storedHash))
                return true;
        }
        catch
        {
            // Tolerancia a usuarios viejos sin formato salt:hash; cae al fallback de plaintext.
        }
        return storedHash == password;
    }
}
