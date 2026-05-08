using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Infrastructure.Persistence;

namespace TicketingSystem.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _context.Reservations.Add(reservation);
        return Task.FromResult(reservation);
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Reservations.FindAsync(new object[] { id }, cancellationToken);

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _context.Reservations.Update(reservation);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Reservation>> GetExpiredReservationsAsync(DateTime now, CancellationToken cancellationToken = default)
        => await _context.Reservations
            .Where(r => r.Status == "Pending" && r.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId, bool onlyPending = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Reservations
            .Where(r => r.UserId == userId);

        if (onlyPending)
            query = query.Where(r => r.Status == "Pending");

        return await query.ToListAsync(cancellationToken);
    }
}