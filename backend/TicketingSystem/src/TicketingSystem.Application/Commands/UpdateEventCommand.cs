namespace TicketingSystem.Application.Commands;

public record UpdateEventCommand(int Id, string Name, DateTime EventDate, string Venue);
