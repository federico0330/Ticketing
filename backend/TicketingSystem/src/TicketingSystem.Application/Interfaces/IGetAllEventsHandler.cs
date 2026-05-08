using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Interfaces;

public interface IGetAllEventsHandler
{
    Task<IEnumerable<EventDto>> HandleAsync(GetAllEventsQuery query);
}
