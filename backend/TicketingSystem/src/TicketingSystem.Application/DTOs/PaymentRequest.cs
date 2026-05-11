namespace TicketingSystem.Application.DTOs;

public class PaymentRequest
{
    public Guid ReservationId { get; set; }
    public string CardToken { get; set; } = string.Empty;
}
