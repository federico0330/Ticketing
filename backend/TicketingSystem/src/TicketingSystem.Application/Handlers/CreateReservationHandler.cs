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

    // Tiempo máximo de reserva en caché antes de liberación automática (Requisito 5)
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
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                Action = "RESERVE_ATTEMPT",
                EntityType = "Seat",
                EntityId = command.SeatId.ToString(),
                Details = JsonSerializer.Serialize(new { command.SeatId, command.UserId }),
                CreatedAt = DateTime.UtcNow
            });

            var seat = await _seatRepository.GetByIdAsync(command.SeatId);
            if (seat is null)
                throw new SeatNotFoundException(command.SeatId);

            if (seat.Status != "Available")
                throw new SeatNotAvailableException(command.SeatId);

            seat.Status = "Reserved";
            
            // Incremento manual de versión para concurrencia mediante Optimistic Locking
            seat.Version += 1; 
            
            await _seatRepository.UpdateAsync(seat);

            var now = DateTime.UtcNow;
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                SeatId = command.SeatId,
                Status = "Pending",
                ReservedAt = now,
                ExpiresAt = now.Add(ReservationDuration)
            };
            await _reservationRepository.CreateAsync(reservation);

            await _auditLogRepository.CreateAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                Action = "RESERVE_SUCCESS",
                EntityType = "Reservation",
                EntityId = reservation.Id.ToString(),
                Details = JsonSerializer.Serialize(new
                {
                    ReservationId = reservation.Id,
                    command.SeatId,
                    command.UserId,
                    reservation.ExpiresAt
                }),
                CreatedAt = DateTime.UtcNow
            });

            // Commit atómico (ACID) asegurando que reserva y auditoría se guarden juntas
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
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogWarning(ex, "[CODE-ERROR] - Intento de reserva en butaca ya tomada (Concurrency Triggered). SeatId: {SeatId}", command.SeatId);
            throw; // Relanza la excepción original de dominio con los datos reales
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
