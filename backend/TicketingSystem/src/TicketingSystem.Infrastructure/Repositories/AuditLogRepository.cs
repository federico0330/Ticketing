using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Infrastructure.Persistence;

namespace TicketingSystem.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task CreateAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }
}