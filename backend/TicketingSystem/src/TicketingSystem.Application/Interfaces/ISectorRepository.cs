using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface ISectorRepository
{
    Task<IEnumerable<Sector>> GetByEventIdAsync(int eventId);
    Task<Sector?> GetByIdAsync(int id);
}