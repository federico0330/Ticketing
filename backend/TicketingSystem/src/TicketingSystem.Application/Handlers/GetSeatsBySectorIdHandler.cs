using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Handlers;

public class GetSeatsBySectorIdHandler
{
    private readonly ISeatRepository _seatRepository;

    public GetSeatsBySectorIdHandler(ISeatRepository seatRepository)
    {
        _seatRepository = seatRepository;
    }

    public async Task<IEnumerable<SeatDto>> HandleAsync(GetSeatsBySectorIdQuery query)
    {
        var seats = await _seatRepository.GetBySectorIdAsync(query.SectorId);

        return seats.Select(s => new SeatDto(
            s.Id,
            s.SectorId,
            s.RowIdentifier,
            s.SeatNumber,
            s.Status
        ));
    }
}