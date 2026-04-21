namespace TicketingSystem.Application.DTOs;

public record ErrorResponse(
    string Error,
    string Message
);