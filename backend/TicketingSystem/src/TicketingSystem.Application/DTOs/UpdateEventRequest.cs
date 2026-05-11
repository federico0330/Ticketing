namespace TicketingSystem.Application.DTOs;

public record UpdateEventRequest(string Name, DateTime EventDate, string Venue);
