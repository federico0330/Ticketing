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

    public async Task<IEnumerable<Seat>> GetBySectorIdAsync(int sectorId, CancellationToken cancellationToken = default)
        => await _context.Seats
            .Where(s => s.SectorId == sectorId)
            .OrderBy(s => s.RowIdentifier)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync(cancellationToken);

    public async Task<Seat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Seats.FindAsync(new object[] { id }, cancellationToken);

    public Task UpdateAsync(Seat seat, CancellationToken cancellationToken = default)
    {
        _context.Seats.Update(seat);
        return Task.CompletedTask;
    }
}