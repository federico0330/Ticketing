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

    public async Task<IEnumerable<Sector>> GetByEventIdAsync(int eventId)
        => await _context.Sectors
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<Sector?> GetByIdAsync(int id)
        => await _context.Sectors.FindAsync(id);
}