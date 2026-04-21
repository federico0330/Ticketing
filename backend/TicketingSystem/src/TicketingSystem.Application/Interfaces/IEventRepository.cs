using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(int id);
}