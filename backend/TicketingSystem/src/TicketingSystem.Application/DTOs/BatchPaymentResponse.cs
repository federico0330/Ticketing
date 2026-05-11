namespace TicketingSystem.Application.DTOs;

public class BatchPaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Guid> PaidReservationIds { get; set; } = new();
}
