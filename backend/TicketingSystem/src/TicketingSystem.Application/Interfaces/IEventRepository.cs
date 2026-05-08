using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}