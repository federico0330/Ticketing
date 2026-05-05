using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TicketingSystem.Infrastructure.BackgroundServices;

public class ReservationExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpirationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public ReservationExpirationWorker(
        IServiceProvider serviceProvider,
        ILogger<ReservationExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpirationWorker is starting.");

        using PeriodicTimer timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired reservations.");
            }
        }

        _logger.LogInformation("ReservationExpirationWorker is stopping.");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<TicketingSystem.Application.Interfaces.IReservationRepository>();
        var seatRepository = scope.ServiceProvider.GetRequiredService<TicketingSystem.Application.Interfaces.ISeatRepository>();
        var auditLogRepository = scope.ServiceProvider.GetRequiredService<TicketingSystem.Application.Interfaces.IAuditLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<TicketingSystem.Application.Interfaces.IUnitOfWork>();

        var expiredReservations = await reservationRepository.GetExpiredReservationsAsync(DateTime.UtcNow);

        foreach (var reservation in expiredReservations)
        {
            await unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.Status = "Expired";
                await reservationRepository.UpdateAsync(reservation);

                var seat = await seatRepository.GetByIdAsync(reservation.SeatId);
                if (seat != null)
                {
                    seat.Status = "Available";
                    await seatRepository.UpdateAsync(seat);
                }

                await auditLogRepository.CreateAsync(new TicketingSystem.Domain.Entities.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = null, // proceso de sistema
                    Action = "RESERVE_EXPIRED",
                    EntityType = "Reservation",
                    EntityId = reservation.Id.ToString(),
                    Details = System.Text.Json.JsonSerializer.Serialize(new { reservation.Id, reservation.SeatId }),
                    CreatedAt = DateTime.UtcNow
                });

                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Reservation {ReservationId} expired and seat {SeatId} released.", reservation.Id, reservation.SeatId);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to expire reservation {ReservationId}.", reservation.Id);
            }
        }
    }
}
