namespace TicketingSystem.Application.DTOs;

public record EventDto(
    int Id,
    string Name,
    DateTime EventDate,
    string Venue,
    string Status
);