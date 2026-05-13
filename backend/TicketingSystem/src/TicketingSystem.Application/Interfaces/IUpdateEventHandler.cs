using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface IUpdateEventHandler
{
    Task<EventDto> HandleAsync(UpdateEventCommand command, CancellationToken cancellationToken = default);
}
