using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetExpiredReservationsAsync(DateTime now, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId, bool onlyPending = true, CancellationToken cancellationToken = default);
}