namespace TicketingSystem.Application.DTOs;

public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    
    // devolvemos el estado final para que la UI se actualice de una sin meter otro request
    public string FinalStatus { get; set; } = string.Empty;
}
