using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface ISectorRepository
{
    Task<IEnumerable<Sector>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Sector?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}