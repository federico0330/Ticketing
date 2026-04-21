using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Application.DTOs;

public record CreateReservationRequest(
    [Required] Guid SeatId,
    [Required] int UserId
);