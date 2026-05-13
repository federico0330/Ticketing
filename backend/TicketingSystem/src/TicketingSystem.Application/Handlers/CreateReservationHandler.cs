using System.Text.Json;
using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class CreateReservationHandler : ICreateReservationHandler
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReservationHandler> _logger;

    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(5);

    public CreateReservationHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateReservationHandler> logger)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReservationDto> HandleAsync(CreateReservationCommand command, CancellationToken cancellationToken = default)
    {
        // Auditamos el intento antes de la transacción para que quede registrado incluso si la reserva falla por concurrencia.
        await LogAuditAsync(command.UserId, "RESERVE_ATTEMPT", "Seat", command.SeatId.ToString(), new
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
            seat.Status = "Reserved";
            seat.Version += 1;
            await _seatRepository.UpdateAsync(seat, cancellationToken);

            var reservation = await BuildReservationAsync(command.UserId, command.SeatId);
            await _reservationRepository.CreateAsync(reservation, cancellationToken);

            await LogAuditAsync(command.UserId, "RESERVE_SUCCESS", "Reservation", reservation.Id.ToString(), new
            {
                ReservationId = reservation.Id,
                command.SeatId,
                command.UserId,
                reservation.ExpiresAt,
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ReservationDto(
                reservation.Id,
                reservation.UserId,
                reservation.SeatId,
                reservation.Status,
                DateTime.SpecifyKind(reservation.ReservedAt, DateTimeKind.Utc),
                DateTime.SpecifyKind(reservation.ExpiresAt, DateTimeKind.Utc)
            );
        }
        catch (ConcurrencyException ex)
        {
            await HandleReservationFailureAsync(command.UserId, command.SeatId, "Concurrency conflict", ex, cancellationToken);
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
        if (seat.Status != "Available")
            throw new SeatNotAvailableException(seatId);
        return seat;
    }

    private Task<Reservation> BuildReservationAsync(int userId, Guid seatId)
    {
        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SeatId = seatId,
            Status = "Pending",
            ReservedAt = now,
            ExpiresAt = now.Add(ReservationDuration)
        };
        return Task.FromResult(reservation);
    }

    private async Task LogAuditAsync(int userId, string action, string entityType, string entityId, object details, CancellationToken cancellationToken)
    {
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task HandleReservationFailureAsync(int userId, Guid seatId, string reason, Exception ex, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        // Limpiamos el ChangeTracker para que el SaveChanges del log de fallo no re-aplique los cambios revertidos.
        _unitOfWork.ClearChanges();
        await LogAuditAsync(userId, "RESERVE_FAILED", "Seat", seatId.ToString(), new
        {
            SeatId = seatId,
            UserId = userId,
            Reason = reason,
            Error = ex.Message,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (reason == "Concurrency conflict")
            _logger.LogWarning(ex, "[CODE-ERROR] - Intento de reserva en butaca ya tomada (Concurrency Triggered). SeatId: {SeatId}", seatId);
    }
}