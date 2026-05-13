namespace TicketingSystem.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(int? userId, string action, string entityType, string entityId, object details, CancellationToken cancellationToken = default);
}
