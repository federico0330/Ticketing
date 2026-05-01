using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;

namespace TicketingSystem.Application.Handlers;

public class LoginHandler : ILoginHandler
{
    private readonly IUserRepository _userRepository;

    public LoginHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<LoginResponse?> HandleAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || user.PasswordHash != request.Password)
        {
            return null;
        }

        return new LoginResponse(user.Id, user.Name, user.Email);
    }
}
