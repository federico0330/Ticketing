namespace TicketingSystem.Domain.Exceptions;

public class EventNotFoundException : Exception
{
    public EventNotFoundException(int eventId)
        : base($"El evento con Id '{eventId}' no fue encontrado.") { }
}
