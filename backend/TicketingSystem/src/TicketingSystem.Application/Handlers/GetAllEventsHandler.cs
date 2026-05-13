using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Handlers;

public class GetAllEventsHandler : IGetAllEventsHandler
{
    private readonly IEventRepository _eventRepository;

    public GetAllEventsHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<EventDto>> HandleAsync(GetAllEventsQuery query, CancellationToken cancellationToken = default)
    {
        var events = await _eventRepository.GetAllAsync(query.IsAdmin, cancellationToken);

        return events.Select(e => new EventDto(
            e.Id,
            e.Name,
            e.EventDate,
            e.Venue,
            e.Status,
            e.Sectors.Count,
            e.Sectors.Sum(s => s.Seats.Count)
        ));
    }
}