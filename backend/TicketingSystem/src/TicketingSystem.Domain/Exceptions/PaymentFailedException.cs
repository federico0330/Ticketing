namespace TicketingSystem.Domain.Exceptions;

public class PaymentFailedException : Exception
{
    public PaymentFailedException(string message) : base(message) { }
}
