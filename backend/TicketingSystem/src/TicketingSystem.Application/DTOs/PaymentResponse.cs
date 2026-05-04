namespace TicketingSystem.Application.DTOs;

public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public string FinalStatus { get; set; } = string.Empty;
}
