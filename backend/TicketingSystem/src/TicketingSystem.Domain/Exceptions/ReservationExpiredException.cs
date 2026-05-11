namespace TicketingSystem.Domain.Exceptions;

public class ReservationExpiredException : Exception
{
    public ReservationExpiredException(Guid reservationId)
        : base($"Reservation {reservationId} has expired and cannot be paid.")
    {
    }
}
