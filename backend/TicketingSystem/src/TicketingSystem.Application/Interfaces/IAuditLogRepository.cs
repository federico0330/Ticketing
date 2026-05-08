using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Interfaces;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}