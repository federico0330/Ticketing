using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface IConfirmPaymentHandler
{
    Task<PaymentResponse> HandleAsync(ConfirmPaymentCommand command);
}
