namespace TicketingSystem.Application.Commands;

public record CreateReservationCommand(Guid SeatId, int UserId);