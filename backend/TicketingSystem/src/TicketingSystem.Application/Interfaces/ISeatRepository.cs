using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface ISeatRepository
{
    Task<IEnumerable<Seat>> GetBySectorIdAsync(int sectorId);
    Task<Seat?> GetByIdAsync(Guid id);
    Task UpdateAsync(Seat seat);
}