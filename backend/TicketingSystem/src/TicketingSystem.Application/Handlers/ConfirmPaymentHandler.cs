using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class ConfirmPaymentHandler : IConfirmPaymentHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(
        IReservationRepository reservationRepository,
        ISeatRepository seatRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmPaymentHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _seatRepository = seatRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentResponse> HandleAsync(ConfirmPaymentCommand command, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Simulación de pasarela de pagos
            if (command.CreditCardNumber.Replace(" ", "").EndsWith("0000"))
            {
                throw new PaymentFailedException("Rechazo bancario simulado. La tarjeta finaliza en 0000.");
            }

            foreach (var reservationId in command.ReservationIds)
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
                if (reservation is null)
                    throw new ReservationNotFoundException(reservationId);

                if (reservation.Status != "Pending")
                    continue; // Skip if already processed or expired

                if (reservation.ExpiresAt <= DateTime.UtcNow)
                    throw new ReservationExpiredException(reservationId);

                reservation.Status = "Paid";
                await _reservationRepository.UpdateAsync(reservation, cancellationToken);

                var seat = await _seatRepository.GetByIdAsync(reservation.SeatId, cancellationToken);
                if (seat != null)
                {
                    seat.Status = "Sold";
                    seat.Version += 1;
                    await _seatRepository.UpdateAsync(seat, cancellationToken);
                }

                await _auditLogRepository.CreateAsync(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = reservation.UserId,
                    Action = "PAYMENT_SUCCESS",
                    EntityType = "Reservation",
                    EntityId = reservation.Id.ToString(),
                    Details = System.Text.Json.JsonSerializer.Serialize(new { reservationId, CardMasked = "****" + command.CreditCardNumber.Substring(Math.Max(0, command.CreditCardNumber.Length - 4)) }),
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new PaymentResponse
            {
                Success = true,
                Message = "Pago procesado exitosamente.",
                FinalStatus = "Paid"
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.ClearChanges();
            _logger.LogError(ex, "[CODE-ERROR] - Fallo al confirmar el pago para las reservas.");
            throw;
        }
    }
}
