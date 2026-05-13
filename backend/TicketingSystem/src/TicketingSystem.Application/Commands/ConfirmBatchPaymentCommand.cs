namespace TicketingSystem.Application.Commands;

public class ConfirmBatchPaymentCommand
{
    public List<Guid> ReservationIds { get; set; } = new();
    public string CardToken { get; set; } = string.Empty;
}
