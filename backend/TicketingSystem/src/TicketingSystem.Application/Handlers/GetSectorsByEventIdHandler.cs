using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class GetSectorsByEventIdHandler : IGetSectorsByEventIdHandler
{
    private readonly ISectorRepository _sectorRepository;
    private readonly IEventRepository _eventRepository;

    public GetSectorsByEventIdHandler(ISectorRepository sectorRepository, IEventRepository eventRepository)
    {
        _sectorRepository = sectorRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<SectorDto>> HandleAsync(GetSectorsByEventIdQuery query)
    {
        var eventExists = await _eventRepository.GetByIdAsync(query.EventId);
        if (eventExists is null)
            throw new EventNotFoundException(query.EventId);

        var sectors = await _sectorRepository.GetByEventIdAsync(query.EventId);

        return sectors.Select(s => new SectorDto(
            s.Id,
            s.EventId,
            s.Name,
            s.Price,
            s.Capacity
        ));
    }
}