using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Infrastructure.Persistence;

namespace TicketingSystem.Infrastructure.Repositories;

public class SectorRepository : ISectorRepository
{
    private readonly AppDbContext _context;

    public SectorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sector>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
        => await _context.Sectors
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<Sector?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Sectors.FindAsync(new object[] { id }, cancellationToken);
}