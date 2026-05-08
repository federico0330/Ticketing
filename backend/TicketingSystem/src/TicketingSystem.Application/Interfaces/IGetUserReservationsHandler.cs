using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Interfaces;

public interface IGetUserReservationsHandler
{
    Task<IEnumerable<ReservationDto>> HandleAsync(GetUserReservationsQuery query);
}
