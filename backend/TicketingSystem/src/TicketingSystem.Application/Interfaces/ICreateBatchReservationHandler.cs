using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
namespace TicketingSystem.Application.Interfaces;

public interface ICreateBatchReservationHandler
{
    Task<BatchReservationResponse> HandleAsync(CreateBatchReservationCommand command, CancellationToken cancellationToken = default);
}
