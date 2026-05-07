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

    public async Task<ReservationDto> HandleAsync(CreateReservationCommand command)
    {
        await LogAuditAsync(command.UserId, "RESERVE_ATTEMPT", "Seat", command.SeatId.ToString(), new { command.SeatId, command.UserId });
        await _unitOfWork.SaveChangesAsync();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var seat = await GetAndValidateSeatAsync(command.SeatId);
            seat.Status = "Reserved";
            seat.Version += 1;
            await _seatRepository.UpdateAsync(seat);

            var reservation = await BuildReservationAsync(command.UserId, command.SeatId);
            await _reservationRepository.CreateAsync(reservation);

            await LogAuditAsync(command.UserId, "RESERVE_SUCCESS", "Reservation", reservation.Id.ToString(), new
            {
                ReservationId = reservation.Id,
                command.SeatId,
                command.UserId,
                reservation.ExpiresAt
            });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new ReservationDto(
                reservation.Id,
                reservation.UserId,
                reservation.SeatId,
                reservation.Status,
                reservation.ReservedAt,
                reservation.ExpiresAt
            );
        }
        catch (ConcurrencyException ex)
        {
            await HandleReservationFailureAsync(command.UserId, command.SeatId, "Concurrency conflict", ex);
            throw;
        }
        catch (Exception ex)
        {
            await HandleReservationFailureAsync(command.UserId, command.SeatId, "Error", ex);
            throw;
        }
    }

    private async Task<Seat> GetAndValidateSeatAsync(Guid seatId)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);
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

    private async Task LogAuditAsync(int userId, string action, string entityType, string entityId, object details)
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
        });
    }

    private async Task HandleReservationFailureAsync(int userId, Guid seatId, string reason, Exception ex)
    {
        await _unitOfWork.RollbackTransactionAsync();
        _unitOfWork.ClearChanges();
        await LogAuditAsync(userId, "RESERVE_FAILED", "Seat", seatId.ToString(), new { seatId, userId, Reason = reason, Error = ex.Message });
        await _unitOfWork.SaveChangesAsync();
        if (reason == "Concurrency conflict")
            _logger.LogWarning(ex, "[CODE-ERROR] - Intento de reserva en butaca ya tomada (Concurrency Triggered). SeatId: {SeatId}", seatId);
    }
}