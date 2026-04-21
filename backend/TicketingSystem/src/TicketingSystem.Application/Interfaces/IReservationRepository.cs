using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation);
}