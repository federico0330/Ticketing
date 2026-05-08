using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Interfaces;

public interface IGetSeatsBySectorIdHandler
{
    Task<IEnumerable<SeatDto>> HandleAsync(GetSeatsBySectorIdQuery query);
}
