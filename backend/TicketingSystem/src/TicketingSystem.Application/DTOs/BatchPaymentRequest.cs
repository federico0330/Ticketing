using System.ComponentModel.DataAnnotations;

namespace TicketingSystem.Application.DTOs;

public class BatchPaymentRequest
{
    [Required]
    public List<Guid> ReservationIds { get; set; } = new();

    [Required]
    public string CardToken { get; set; } = string.Empty;
}
