using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(Guid id);
    Task UpdateAsync(Reservation reservation);
    Task<IEnumerable<Reservation>> GetExpiredReservationsAsync(DateTime now);
}