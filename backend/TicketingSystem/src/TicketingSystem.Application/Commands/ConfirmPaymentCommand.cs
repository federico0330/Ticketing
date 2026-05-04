namespace TicketingSystem.Application.Commands;

public class ConfirmPaymentCommand
{
    public Guid ReservationId { get; set; }
    public string CardToken { get; set; } = string.Empty;
}
