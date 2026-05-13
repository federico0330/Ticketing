using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
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

    public async Task<IEnumerable<Event>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Include(e => e.Sectors)
                .ThenInclude(s => s.Seats)
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(e => e.Status == EventStatus.Active);

        return await query.OrderBy(e => e.EventDate).ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Events.FindAsync(new object[] { id }, cancellationToken);

    public Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _context.Events.Add(@event);
        return Task.FromResult(@event);
    }

    public Task UpdateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _context.Events.Update(@event);
        return Task.CompletedTask;
    }
}