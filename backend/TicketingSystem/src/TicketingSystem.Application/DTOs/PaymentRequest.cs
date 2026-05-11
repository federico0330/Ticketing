using System;
using System.Collections.Generic;

namespace TicketingSystem.Application.DTOs;

public class PaymentRequest
{
    public List<Guid> ReservationIds { get; set; } = new();
    public string CreditCardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
}
