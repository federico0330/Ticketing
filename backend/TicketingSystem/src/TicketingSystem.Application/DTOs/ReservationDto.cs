namespace TicketingSystem.Application.DTOs;

public record ReservationDto(
    Guid Id,
    int UserId,
    Guid SeatId,
    string Status,
    DateTime ReservedAt,
    DateTime ExpiresAt
);