namespace TicketingSystem.Domain.Exceptions;

public class SeatNotFoundException : Exception
{
    public SeatNotFoundException(Guid seatId)
        : base($"El asiento con Id '{seatId}' no fue encontrado.") { }
}
