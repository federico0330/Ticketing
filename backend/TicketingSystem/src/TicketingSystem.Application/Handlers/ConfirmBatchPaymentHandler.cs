using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;

namespace TicketingSystem.Application.Handlers;

public class ConfirmBatchPaymentHandler : IConfirmBatchPaymentHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmBatchPaymentHandler> _logger;

    public ConfirmBatchPaymentHandler(
        IReservationRepository reservationRepository,
        ISeatRepository seatRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmBatchPaymentHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _seatRepository = seatRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BatchPaymentResponse> HandleAsync(ConfirmBatchPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ReservationIds == null || command.ReservationIds.Count == 0)
            throw new ArgumentException("No reservations provided for batch payment.");

        int? userId = null;
        var paidIds = new List<Guid>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservations = new List<Domain.Entities.Reservation>();
            foreach (var reservationId in command.ReservationIds)
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
                if (reservation is null)
                    throw new Domain.Exceptions.ReservationNotFoundException(reservationId);

                if (reservation.Status != "Pending")
                    throw new InvalidOperationException($"Reservation {reservationId} is not in a pending state.");

                if (reservation.ExpiresAt <= DateTime.UtcNow)
                    throw new Domain.Exceptions.ReservationExpiredException(reservationId);

                userId ??= reservation.UserId;
                reservations.Add(reservation);
            }

            foreach (var reservation in reservations)
            {
                reservation.Status = "Paid";
                await _reservationRepository.UpdateAsync(reservation, cancellationToken);

                var seat = await _seatRepository.GetByIdAsync(reservation.SeatId, cancellationToken);
                if (seat == null)
                    throw new InvalidOperationException($"Seat {reservation.SeatId} not found for reservation.");

                seat.Status = "Sold";
                seat.Version += 1;
                await _seatRepository.UpdateAsync(seat, cancellationToken);

                var auditLog = new Domain.Entities.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = reservation.UserId,
                    Action = "PAYMENT_SUCCESS",
                    EntityType = "Reservation",
                    EntityId = reservation.Id.ToString(),
                    Details = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ReservationId = reservation.Id,
                        command.CardToken,
                        BatchSize = command.ReservationIds.Count,
                        TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }),
                    CreatedAt = DateTime.UtcNow
                };
                await _auditLogRepository.CreateAsync(auditLog, cancellationToken);

                paidIds.Add(reservation.Id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new BatchPaymentResponse
            {
                Success = true,
                Message = $"Batch payment processed successfully for {paidIds.Count} reservation(s).",
                PaidReservationIds = paidIds
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearChanges();

            await _auditLogRepository.CreateAsync(new Domain.Entities.AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = "PAYMENT_FAILED",
                EntityType = "Reservation",
                EntityId = string.Join(",", command.ReservationIds),
                Details = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ReservationIds = command.ReservationIds,
                    Error = ex.Message,
                    TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Batch payment failed for reservations {ReservationIds}.", string.Join(",", command.ReservationIds));
            throw;
        }
    }
}
