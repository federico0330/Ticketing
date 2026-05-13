using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class CreateReservationHandler : ICreateReservationHandler
{
    private const string ConcurrencyConflictReason = "Concurrency conflict";
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(5);

    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReservationHandler> _logger;

    public CreateReservationHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        ILogger<CreateReservationHandler> logger)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReservationDto> HandleAsync(CreateReservationCommand command, CancellationToken cancellationToken = default)
    {
        // Auditamos el intento antes de la transacción para que quede registrado incluso si la reserva falla por concurrencia.
        await _auditLogger.LogAsync(command.UserId, AuditAction.ReserveAttempt, "Seat", command.SeatId.ToString(), new
        {
            command.SeatId,
            command.UserId,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var seat = await GetAndValidateSeatAsync(command.SeatId, cancellationToken);
            seat.Status = SeatStatus.Reserved;
            seat.Version += 1;
            await _seatRepository.UpdateAsync(seat, cancellationToken);

            var reservation = BuildReservation(command.UserId, command.SeatId);
            await _reservationRepository.CreateAsync(reservation, cancellationToken);

            await _auditLogger.LogAsync(command.UserId, AuditAction.ReserveSuccess, "Reservation", reservation.Id.ToString(), new
            {
                ReservationId = reservation.Id,
                command.SeatId,
                command.UserId,
                reservation.ExpiresAt,
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ToDto(reservation);
        }
        catch (ConcurrencyException ex)
        {
            await HandleReservationFailureAsync(command.UserId, command.SeatId, ConcurrencyConflictReason, ex, cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await HandleReservationFailureAsync(command.UserId, command.SeatId, "Error", ex, cancellationToken);
            throw;
        }
    }

    private async Task<Seat> GetAndValidateSeatAsync(Guid seatId, CancellationToken cancellationToken)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId, cancellationToken);
        if (seat is null)
            throw new SeatNotFoundException(seatId);
        if (seat.Status != SeatStatus.Available)
            throw new SeatNotAvailableException(seatId);
        return seat;
    }

    private static Reservation BuildReservation(int userId, Guid seatId)
    {
        var now = DateTime.UtcNow;
        return new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SeatId = seatId,
            Status = ReservationStatus.Pending,
            ReservedAt = now,
            ExpiresAt = now.Add(ReservationDuration)
        };
    }

    private static ReservationDto ToDto(Reservation reservation) => new(
        reservation.Id,
        reservation.UserId,
        reservation.SeatId,
        reservation.Status,
        DateTime.SpecifyKind(reservation.ReservedAt, DateTimeKind.Utc),
        DateTime.SpecifyKind(reservation.ExpiresAt, DateTimeKind.Utc)
    );

    private async Task HandleReservationFailureAsync(int userId, Guid seatId, string reason, Exception ex, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        // Limpiamos el ChangeTracker para que el SaveChanges del log de fallo no re-aplique los cambios revertidos.
        _unitOfWork.ClearChanges();
        await _auditLogger.LogAsync(userId, AuditAction.ReserveFailed, "Seat", seatId.ToString(), new
        {
            SeatId = seatId,
            UserId = userId,
            Reason = reason,
            Error = ex.Message,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (reason == ConcurrencyConflictReason)
            _logger.LogWarning(ex, "[CODE-ERROR] - Intento de reserva en butaca ya tomada (Concurrency Triggered). SeatId: {SeatId}", seatId);
    }
}
