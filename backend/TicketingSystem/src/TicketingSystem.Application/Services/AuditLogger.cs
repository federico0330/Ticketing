using System.Text.Json;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogger(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public Task LogAsync(int? userId, string action, string entityType, string entityId, object details, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow
        };
        return _auditLogRepository.CreateAsync(auditLog, cancellationToken);
    }
}
