using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface ILoginHandler
{
    Task<LoginResponse?> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
