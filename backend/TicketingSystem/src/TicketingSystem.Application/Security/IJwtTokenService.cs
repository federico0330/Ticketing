using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Security;

public interface IJwtTokenService
{
    string GenerateToken(User user, string role);
    string ResolveRole(string email);
}
