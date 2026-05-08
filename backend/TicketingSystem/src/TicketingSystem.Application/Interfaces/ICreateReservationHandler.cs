using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface ICreateReservationHandler
{
    Task<ReservationDto> HandleAsync(CreateReservationCommand command);
}
