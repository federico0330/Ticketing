using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface ISeatRepository
{
    Task<IEnumerable<Seat>> GetBySectorIdAsync(int sectorId, CancellationToken cancellationToken = default);
    Task<Seat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Seat seat, CancellationToken cancellationToken = default);
}