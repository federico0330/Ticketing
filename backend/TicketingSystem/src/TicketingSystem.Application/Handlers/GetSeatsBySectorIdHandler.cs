using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Handlers;

public class GetSeatsBySectorIdHandler : IGetSeatsBySectorIdHandler
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;

    public GetSeatsBySectorIdHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<IEnumerable<SeatDto>> HandleAsync(GetSeatsBySectorIdQuery query)
    {
        var seats = await _seatRepository.GetBySectorIdAsync(query.SectorId);

        HashSet<Guid>? userReservedSeatIds = null;
        if (query.CurrentUserId.HasValue)
        {
            var userReservations = await _reservationRepository.GetByUserIdAsync(query.CurrentUserId.Value, onlyPending: true);
            userReservedSeatIds = new HashSet<Guid>(
                userReservations.Where(r => r.Status == "Pending").Select(r => r.SeatId)
            );
        }

        return seats.Select(s => new SeatDto(
            s.Id,
            s.SectorId,
            s.RowIdentifier,
            s.SeatNumber,
            s.Status,
            userReservedSeatIds != null && userReservedSeatIds.Contains(s.Id)
        ));
    }
}