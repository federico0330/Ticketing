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

    public Task<Reservation> CreateAsync(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
        return Task.FromResult(reservation);
    }

    public async Task<Reservation?> GetByIdAsync(Guid id)
        => await _context.Reservations.FindAsync(id);

    public Task UpdateAsync(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Reservation>> GetExpiredReservationsAsync(DateTime now)
        => await _context.Reservations
            .Where(r => r.Status == "Pending" && r.ExpiresAt <= now)
            .ToListAsync();
}