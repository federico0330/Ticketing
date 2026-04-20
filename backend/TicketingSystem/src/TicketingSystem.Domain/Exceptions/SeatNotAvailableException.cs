namespace TicketingSystem.Domain.Exceptions;

public class SeatNotAvailableException : Exception
{
    public SeatNotAvailableException(Guid seatId)
        : base($"El asiento con Id '{seatId}' no está disponible para reserva.") { }
}
