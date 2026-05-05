using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;

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

    public async Task<PaymentResponse> HandleAsync(ConfirmPaymentCommand command)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId);
            if (reservation is null)
                throw new Domain.Exceptions.ReservationNotFoundException(command.ReservationId);

            if (reservation.Status != "Pending")
                throw new InvalidOperationException($"Reservation {command.ReservationId} is not in a pending state.");

            if (reservation.ExpiresAt <= DateTime.UtcNow)
                throw new Domain.Exceptions.ReservationExpiredException(command.ReservationId);

            reservation.Status = "Paid";
            await _reservationRepository.UpdateAsync(reservation);

            var seat = await _seatRepository.GetByIdAsync(reservation.SeatId);
            seat.Status = "Sold";
            await _seatRepository.UpdateAsync(seat);

            var auditLog = new Domain.Entities.AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = reservation.UserId,
                Action = "PAYMENT_SUCCESS",
                EntityType = "Reservation",
                EntityId = reservation.Id.ToString(),
                Details = System.Text.Json.JsonSerializer.Serialize(new { command.ReservationId, command.CardToken }),
                CreatedAt = DateTime.UtcNow
            };
            await _auditLogRepository.CreateAsync(auditLog);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new PaymentResponse
            {
                Success = true,
                Message = "Payment processed successfully.",
                ReservationId = reservation.Id,
                FinalStatus = "Paid"
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Payment confirmation failed for reservation {ReservationId}.", command.ReservationId);
            throw;
        }
    }
}
