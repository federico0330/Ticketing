using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Infrastructure.Persistence;

namespace TicketingSystem.Infrastructure.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly AppDbContext _context;

    public SeatRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Seat>> GetBySectorIdAsync(int sectorId)
        => await _context.Seats
            .Where(s => s.SectorId == sectorId)
            .OrderBy(s => s.RowIdentifier)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();

    public async Task<Seat?> GetByIdAsync(Guid id)
        => await _context.Seats.FindAsync(id);

    public async Task UpdateAsync(Seat seat)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync();
    }
}