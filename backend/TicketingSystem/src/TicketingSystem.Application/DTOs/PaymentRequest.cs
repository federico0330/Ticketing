namespace TicketingSystem.Application.DTOs;

public class PaymentRequest
{
    public Guid ReservationId { get; set; }
    
    // pedimos solo el token para que nuestro backend no manipule datos crudos de tarjetas
    public string CardToken { get; set; } = string.Empty;
}
