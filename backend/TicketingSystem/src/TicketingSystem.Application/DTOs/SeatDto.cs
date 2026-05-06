namespace TicketingSystem.Application.DTOs;

public record SeatDto(
    Guid Id,
    int SectorId,
    string RowIdentifier,
    int SeatNumber,
    string Status,
    bool IsReservedByCurrentUser = false
);