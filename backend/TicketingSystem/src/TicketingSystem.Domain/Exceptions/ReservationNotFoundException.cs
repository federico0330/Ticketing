namespace TicketingSystem.Domain.Exceptions;

public class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(Guid reservationId)
        : base($"Reservation {reservationId} was not found.")
    {
    }
}
