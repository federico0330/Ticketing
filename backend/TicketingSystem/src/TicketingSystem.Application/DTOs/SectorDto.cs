namespace TicketingSystem.Application.DTOs;

public record SectorDto(
    int Id,
    int EventId,
    string Name,
    decimal Price,
    int Capacity
);