using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class ConfirmBatchPaymentHandler : IConfirmBatchPaymentHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmBatchPaymentHandler> _logger;

    public ConfirmBatchPaymentHandler(
        IReservationRepository reservationRepository,
        ISeatRepository seatRepository,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmBatchPaymentHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _seatRepository = seatRepository;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BatchPaymentResponse> HandleAsync(ConfirmBatchPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ReservationIds == null || command.ReservationIds.Count == 0)
            throw new ArgumentException("No reservations provided for batch payment.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservations = await LoadAndValidatePendingReservationsAsync(command.ReservationIds, cancellationToken);
            var paidIds = await ProcessReservationsAsync(reservations, command.CardToken, command.ReservationIds.Count, cancellationToken);

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
            await HandleBatchPaymentFailureAsync(command.ReservationIds, ex, cancellationToken);
            throw;
        }
    }

    private async Task<List<Reservation>> LoadAndValidatePendingReservationsAsync(List<Guid> reservationIds, CancellationToken cancellationToken)
    {
        var reservations = new List<Reservation>();
        foreach (var reservationId in reservationIds)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
            if (reservation is null)
                throw new ReservationNotFoundException(reservationId);

            if (reservation.Status != ReservationStatus.Pending)
                throw new InvalidOperationException($"Reservation {reservationId} is not in a pending state.");

            if (reservation.ExpiresAt <= DateTime.UtcNow)
                throw new ReservationExpiredException(reservationId);

            reservations.Add(reservation);
        }
        return reservations;
    }

    private async Task<List<Guid>> ProcessReservationsAsync(List<Reservation> reservations, string cardToken, int batchSize, CancellationToken cancellationToken)
    {
        var paidIds = new List<Guid>();
        foreach (var reservation in reservations)
        {
            reservation.Status = ReservationStatus.Paid;
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);

            var seat = await _seatRepository.GetByIdAsync(reservation.SeatId, cancellationToken)
                ?? throw new InvalidOperationException($"Seat {reservation.SeatId} not found for reservation.");

            seat.Status = SeatStatus.Sold;
            seat.Version += 1;
            await _seatRepository.UpdateAsync(seat, cancellationToken);

            await _auditLogger.LogAsync(reservation.UserId, AuditAction.PaymentSuccess, "Reservation", reservation.Id.ToString(), new
            {
                ReservationId = reservation.Id,
                CardToken = cardToken,
                BatchSize = batchSize,
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, cancellationToken);

            paidIds.Add(reservation.Id);
        }
        return paidIds;
    }

    private async Task HandleBatchPaymentFailureAsync(List<Guid> reservationIds, Exception ex, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearChanges();

        await _auditLogger.LogAsync(null, AuditAction.PaymentFailed, "Reservation", string.Join(",", reservationIds), new
        {
            ReservationIds = reservationIds,
            Error = ex.Message,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogError(ex, "Batch payment failed for reservations {ReservationIds}.", string.Join(",", reservationIds));
    }
}
