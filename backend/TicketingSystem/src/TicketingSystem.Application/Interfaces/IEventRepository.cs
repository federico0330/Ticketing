using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event @event, CancellationToken cancellationToken = default);
}