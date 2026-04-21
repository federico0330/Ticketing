using System.Text.Json;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class CreateReservationHandler
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    // El tiempo de expiración de la reserva: 5 minutos (configurable aquí)
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(5);

    public CreateReservationHandler(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IAuditLogRepository auditLogRepository)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ReservationDto> HandleAsync(CreateReservationCommand command)
    {
        // Paso 1: Registrar el INTENTO de reserva en auditoría (siempre, incluso si falla)
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

        // Paso 2: Verificar que el asiento existe
        var seat = await _seatRepository.GetByIdAsync(command.SeatId);
        if (seat is null)
            throw new SeatNotFoundException(command.SeatId);

        // Paso 3: Verificar que el asiento está disponible
        if (seat.Status != "Available")
            throw new SeatNotAvailableException(command.SeatId);

        // Paso 4: Cambiar el estado del asiento a "Reserved"
        seat.Status = "Reserved";
        seat.Version += 1; // Incrementar versión para soporte de Optimistic Locking futuro
        await _seatRepository.UpdateAsync(seat);

        // Paso 5: Crear la reserva con fecha de expiración en 5 minutos
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

        // Paso 6: Registrar el ÉXITO de la reserva en auditoría
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

        // Paso 7: Retornar el DTO con los datos de la reserva creada
        return new ReservationDto(
            reservation.Id,
            reservation.UserId,
            reservation.SeatId,
            reservation.Status,
            reservation.ReservedAt,
            reservation.ExpiresAt
        );
    }
}