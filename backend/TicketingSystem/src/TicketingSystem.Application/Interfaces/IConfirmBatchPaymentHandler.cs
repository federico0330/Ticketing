using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface IConfirmBatchPaymentHandler
{
    Task<BatchPaymentResponse> HandleAsync(ConfirmBatchPaymentCommand command, CancellationToken cancellationToken = default);
}
