namespace TicketingSystem.Application.DTOs;

public record LoginResponse(
    int Id,
    string Name,
    string Email,
    string Token,
    string Role
);