using System.Text.Json;
using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class CreateBatchReservationHandler : ICreateBatchReservationHandler
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateBatchReservationHandler> _logger;

    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(5);

    public CreateBatchReservationHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateBatchReservationHandler> logger)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BatchReservationResponse> HandleAsync(CreateBatchReservationCommand command, CancellationToken cancellationToken = default)
    {
        var seatIds = command.SeatIds.Distinct().ToList();
        if (seatIds.Count == 0)
            throw new ArgumentException("El lote debe contener al menos una butaca.");

        await LogAuditAsync(command.UserId, "RESERVE_ATTEMPT", "Seat", string.Join(",", seatIds), new
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
                await LogAuditAsync(command.UserId, "RESERVE_SUCCESS", "Reservation", reservation.Id.ToString(), new
                {
                    ReservationId = reservation.Id,
                    reservation.SeatId,
                    command.UserId,
                    reservation.ExpiresAt,
                    TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var dtos = reservations.Select(r => new ReservationDto(r.Id, r.UserId, r.SeatId, r.Status, r.ReservedAt, r.ExpiresAt)).ToList();
            return new BatchReservationResponse { Success = true, Message = $"Reservaste {seatIds.Count} butaca(s).", Reservations = dtos };
        }
        catch (ConcurrencyException ex)
        {
            await HandleBatchFailureAsync("Concurrency conflict", ex, command, seatIds, cancellationToken);
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
        if (seat.Status != "Available") throw new SeatNotAvailableException(seatId);

        seat.Status = "Reserved";
        seat.Version += 1;
        await _seatRepository.UpdateAsync(seat, cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SeatId = seatId,
            Status = "Pending",
            ReservedAt = now,
            ExpiresAt = now.Add(ReservationDuration)
        };
        await _reservationRepository.CreateAsync(reservation, cancellationToken);
        return reservation;
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

    private async Task HandleBatchFailureAsync(string reason, Exception ex, CreateBatchReservationCommand command, List<Guid> seatIds, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearChanges();
        await LogAuditAsync(command.UserId, "RESERVE_FAILED", "Seat", string.Join(",", seatIds), new
        {
            SeatIds = seatIds,
            command.UserId,
            Reason = reason,
            Error = ex.Message,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (reason == "Concurrency conflict")
            _logger.LogWarning(ex, "[CODE-ERROR] - Intento de reserva en lote con butaca ya tomada (Concurrency Triggered). SeatIds: {SeatIds}", string.Join(",", seatIds));
    }
}
