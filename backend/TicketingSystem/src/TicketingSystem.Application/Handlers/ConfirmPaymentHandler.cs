using Microsoft.Extensions.Logging;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class ConfirmPaymentHandler : IConfirmPaymentHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(
        IReservationRepository reservationRepository,
        ISeatRepository seatRepository,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmPaymentHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _seatRepository = seatRepository;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentResponse> HandleAsync(ConfirmPaymentCommand command, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Sin pasarela real: las tarjetas que terminan en 0000 se rechazan para poder demostrar el flujo de error.
            if (command.CreditCardNumber.Replace(" ", "").EndsWith("0000"))
                throw new PaymentFailedException("Rechazo bancario simulado. La tarjeta finaliza en 0000.");

            foreach (var reservationId in command.ReservationIds)
                await PayReservationAsync(reservationId, command.CreditCardNumber, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new PaymentResponse
            {
                Success = true,
                Message = "Pago procesado exitosamente.",
                FinalStatus = ReservationStatus.Paid
            };
        }
        catch (Exception ex)
        {
            await HandlePaymentFailureAsync(command.ReservationIds, ex, cancellationToken);
            throw;
        }
    }

    private async Task PayReservationAsync(Guid reservationId, string creditCardNumber, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation is null)
            throw new ReservationNotFoundException(reservationId);

        // Si una reserva del lote ya cambió de estado (otro tab, expiración, etc.) la saltamos para no romper las demás.
        if (reservation.Status != ReservationStatus.Pending)
            return;

        if (reservation.ExpiresAt <= DateTime.UtcNow)
            throw new ReservationExpiredException(reservationId);

        reservation.Status = ReservationStatus.Paid;
        await _reservationRepository.UpdateAsync(reservation, cancellationToken);

        var seat = await _seatRepository.GetByIdAsync(reservation.SeatId, cancellationToken);
        if (seat != null)
        {
            seat.Status = SeatStatus.Sold;
            seat.Version += 1;
            await _seatRepository.UpdateAsync(seat, cancellationToken);
        }

        await _auditLogger.LogAsync(reservation.UserId, AuditAction.PaymentSuccess, "Reservation", reservation.Id.ToString(), new
        {
            ReservationId = reservation.Id,
            // Guardamos solo los últimos 4 dígitos: no debe quedar nunca el número completo en logs.
            CardMasked = "****" + creditCardNumber[Math.Max(0, creditCardNumber.Length - 4)..],
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
    }

    private async Task HandlePaymentFailureAsync(List<Guid> reservationIds, Exception ex, CancellationToken cancellationToken)
    {
        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        _unitOfWork.ClearChanges();
        _logger.LogError(ex, "[CODE-ERROR] - Fallo al confirmar el pago para las reservas.");
        await _auditLogger.LogAsync(null, AuditAction.PaymentFailed, "Reservation", string.Join(",", reservationIds), new
        {
            ReservationIds = reservationIds,
            Error = ex.Message,
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
