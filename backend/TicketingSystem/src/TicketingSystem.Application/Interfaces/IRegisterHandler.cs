using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface IRegisterHandler
{
    Task<LoginResponse?> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}