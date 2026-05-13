using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class CreateBatchReservationHandler : ICreateBatchReservationHandler
{
    private const string ConcurrencyConflictReason = "Concurrency conflict";
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(5);

    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateBatchReservationHandler> _logger;

    public CreateBatchReservationHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        ILogger<CreateBatchReservationHandler> logger)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BatchReservationResponse> HandleAsync(CreateBatchReservationCommand command, CancellationToken cancellationToken = default)
    {
        var seatIds = command.SeatIds.Distinct().ToList();
        if (seatIds.Count == 0)
            throw new ArgumentException("El lote debe contener al menos una butaca.");

        await _auditLogger.LogAsync(command.UserId, AuditAction.ReserveAttempt, "Seat", string.Join(",", seatIds), new
        {
            SeatIds = seatIds,
            command.UserId,
            BatchSize = seatIds.Count,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var reservations = new List<Reservation>();

            foreach (var seatId in seatIds)
                reservations.Add(await ValidateAndReserveSeatAsync(seatId, command.UserId, now, cancellationToken));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var reservation in reservations)
                await _auditLogger.LogAsync(command.UserId, AuditAction.ReserveSuccess, "Reservation", reservation.Id.ToString(), new
                {
                    ReservationId = reservation.Id,
                    reservation.SeatId,
                    command.UserId,
                    reservation.ExpiresAt,
                    TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new BatchReservationResponse
            {
                Success = true,
                Message = $"Reservaste {seatIds.Count} butaca(s).",
                Reservations = reservations.Select(ToDto).ToList()
            };
        }
        catch (ConcurrencyException ex)
        {
            await HandleBatchFailureAsync(ConcurrencyConflictReason, ex, command, seatIds, cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await HandleBatchFailureAsync("Error", ex, command, seatIds, cancellationToken);
            throw;
        }
    }

    private async Task<Reservation> ValidateAndReserveSeatAsync(Guid seatId, int userId, DateTime now, CancellationToken cancellationToken)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId, cancellationToken);
        if (seat is null) throw new SeatNotFoundException(seatId);
        if (seat.Status != SeatStatus.Available) throw new SeatNotAvailableException(seatId);

        seat.Status = SeatStatus.Reserved;
        seat.Version += 1;
        await _seatRepository.UpdateAsync(seat, cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SeatId = seatId,
            Status = ReservationStatus.Pending,
            ReservedAt = now,
            ExpiresAt = now.Add(ReservationDuration)
        };
        await _reservationRepository.CreateAsync(reservation, cancellationToken);
        return reservation;
    }

    private static ReservationDto ToDto(Reservation reservation) => new(
        reservation.Id,
        reservation.UserId,
        reservation.SeatId,
        reservation.Status,
        DateTime.SpecifyKind(reservation.ReservedAt, DateTimeKind.Utc),
        DateTime.SpecifyKind(reservation.ExpiresAt, DateTimeKind.Utc)
    );

    private async Task HandleBatchFailureAsync(string reason, Exception ex, CreateBatchReservationCommand command, List<Guid> seatIds, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearChanges();
        await _auditLogger.LogAsync(command.UserId, AuditAction.ReserveFailed, "Seat", string.Join(",", seatIds), new
        {
            SeatIds = seatIds,
            command.UserId,
            Reason = reason,
            Error = ex.Message,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (reason == ConcurrencyConflictReason)
            _logger.LogWarning(ex, "[CODE-ERROR] - Intento de reserva en lote con butaca ya tomada (Concurrency Triggered). SeatIds: {SeatIds}", string.Join(",", seatIds));
    }
}
