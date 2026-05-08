using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Interfaces;

public interface IGetSectorsByEventIdHandler
{
    Task<IEnumerable<SectorDto>> HandleAsync(GetSectorsByEventIdQuery query, CancellationToken cancellationToken = default);
}
