using TicketingSystem.Application.Commands;

namespace TicketingSystem.Application.Interfaces;

public interface IDeleteEventHandler
{
    Task HandleAsync(DeleteEventCommand command, CancellationToken cancellationToken = default);
}
