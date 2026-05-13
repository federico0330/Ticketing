using System.Threading;
using System.Threading.Tasks;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Interfaces;

public interface ICreateEventHandler
{
    Task<EventDto> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken = default);
}
