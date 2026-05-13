using System.ComponentModel.DataAnnotations;
namespace TicketingSystem.Application.DTOs;

public class BatchReservationRequest
{
    [Required, MinLength(1)] public List<Guid> SeatIds { get; set; } = new();
    [Required] public int UserId { get; set; }
}
