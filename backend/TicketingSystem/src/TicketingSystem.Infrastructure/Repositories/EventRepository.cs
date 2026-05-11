using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Infrastructure.Persistence;

namespace TicketingSystem.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Events.OrderBy(e => e.EventDate).ToListAsync(cancellationToken);

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Events.FindAsync(new object[] { id }, cancellationToken);

    public Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _context.Events.Add(@event);
        return Task.FromResult(@event);
    }
}