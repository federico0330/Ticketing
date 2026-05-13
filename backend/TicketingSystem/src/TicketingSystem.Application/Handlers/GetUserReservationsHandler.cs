using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Application.Handlers;

public class GetUserReservationsHandler : IGetUserReservationsHandler
{
    private readonly IReservationRepository _reservationRepository;

    public GetUserReservationsHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<IEnumerable<ReservationDto>> HandleAsync(GetUserReservationsQuery query, CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetByUserIdAsync(query.UserId, query.OnlyPending, cancellationToken);

        return reservations.Select(r => new ReservationDto(
            r.Id,
            r.UserId,
            r.SeatId,
            r.Status,
            DateTime.SpecifyKind(r.ReservedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(r.ExpiresAt, DateTimeKind.Utc)
        ));
    }
}
