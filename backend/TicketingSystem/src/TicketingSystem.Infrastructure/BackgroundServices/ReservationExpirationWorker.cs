using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Infrastructure.BackgroundServices;

public class ReservationExpirationWorker : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpirationWorker> _logger;

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

        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CODE-ERROR] - Error occurred while processing expired reservations.");
            }
        }

        _logger.LogInformation("ReservationExpirationWorker is stopping.");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken stoppingToken)
    {
        // Scope nuevo por tick: el DbContext es scoped y no puede compartirse entre ejecuciones del background service.
        using var scope = _serviceProvider.CreateScope();
        var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var seatRepository = scope.ServiceProvider.GetRequiredService<ISeatRepository>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiredReservations = await reservationRepository.GetExpiredReservationsAsync(DateTime.UtcNow, stoppingToken);

        foreach (var reservation in expiredReservations)
            await ExpireReservationSafelyAsync(reservation, reservationRepository, seatRepository, auditLogger, unitOfWork, stoppingToken);
    }

    private async Task ExpireReservationSafelyAsync(
        Reservation reservation,
        IReservationRepository reservationRepository,
        ISeatRepository seatRepository,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        // Transacción por reserva: si una falla no arrastramos al resto del lote.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            reservation.Status = ReservationStatus.Expired;
            await reservationRepository.UpdateAsync(reservation, cancellationToken);

            var seat = await seatRepository.GetByIdAsync(reservation.SeatId, cancellationToken);
            if (seat != null)
            {
                seat.Status = SeatStatus.Available;
                seat.Version += 1;
                await seatRepository.UpdateAsync(seat, cancellationToken);
            }

            // UserId null porque la acción es del sistema, no de un usuario; queda explícito en auditoría.
            await auditLogger.LogAsync(null, AuditAction.ReservationExpired, "Reservation", reservation.Id.ToString(), new
            {
                reservation.Id,
                reservation.SeatId,
                Status = "SUCCESS",
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Reservation {ReservationId} expired and seat {SeatId} released.", reservation.Id, reservation.SeatId);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            unitOfWork.ClearChanges();
            _logger.LogError(ex, "[CODE-ERROR] - Failed to expire reservation {ReservationId}.", reservation.Id);
            try
            {
                await auditLogger.LogAsync(null, AuditAction.ReservationExpired, "Reservation", reservation.Id.ToString(), new
                {
                    reservation.Id,
                    reservation.SeatId,
                    Status = "FAILED",
                    Error = ex.Message,
                    TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "[CODE-ERROR] - No se pudo registrar la auditoría de fallo de expiración.");
            }
        }
    }
}
