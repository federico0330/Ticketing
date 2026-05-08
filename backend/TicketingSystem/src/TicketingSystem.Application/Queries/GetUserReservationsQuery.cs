namespace TicketingSystem.Application.Queries;

public record GetUserReservationsQuery(int UserId, bool OnlyPending = true);
