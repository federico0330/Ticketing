using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Presentation.Controllers;

[ApiController]
[Route("api/v1/reservations")]
[Produces("application/json")]
public class ReservationsController : ControllerBase
{
    private readonly IConfirmPaymentHandler _confirmPaymentHandler;
    private readonly IConfirmBatchPaymentHandler _confirmBatchPaymentHandler;
    private readonly IGetUserReservationsHandler _getUserReservationsHandler;
    private readonly ICreateBatchReservationHandler _createBatchReservationHandler;

    public ReservationsController(
        IConfirmPaymentHandler confirmPaymentHandler,
        IConfirmBatchPaymentHandler confirmBatchPaymentHandler,
        IGetUserReservationsHandler getUserReservationsHandler,
        ICreateBatchReservationHandler createBatchReservationHandler)
    {
        _confirmPaymentHandler = confirmPaymentHandler;
        _confirmBatchPaymentHandler = confirmBatchPaymentHandler;
        _getUserReservationsHandler = getUserReservationsHandler;
        _createBatchReservationHandler = createBatchReservationHandler;
    }

    [HttpPost("payments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPayment([FromBody] PaymentRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfirmPaymentCommand 
        { 
            ReservationIds = request.ReservationIds, 
            CreditCardNumber = request.CreditCardNumber,
            CardHolderName = request.CardHolderName,
            ExpiryDate = request.ExpiryDate,
            Cvv = request.Cvv
        };

        var response = await _confirmPaymentHandler.HandleAsync(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("batch-payments")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmBatchPayment([FromBody] BatchPaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.ReservationIds == null || request.ReservationIds.Count == 0)
        {
            return BadRequest(new { Error = "EMPTY_BATCH", Message = "Debés enviar al menos una reserva." });
        }

        var command = new ConfirmBatchPaymentCommand
        {
            ReservationIds = request.ReservationIds,
            CardToken = request.CardToken
        };

        var response = await _confirmBatchPaymentHandler.HandleAsync(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("batch")]
    [Authorize]
    [ProducesResponseType(typeof(BatchReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBatch([FromBody] BatchReservationRequest request, CancellationToken cancellationToken)
    {
        if (request.SeatIds is null || request.SeatIds.Count == 0)
            return BadRequest(new { Error = "EMPTY_BATCH", Message = "Debés enviar al menos una butaca." });

        var command = new CreateBatchReservationCommand { SeatIds = request.SeatIds, UserId = request.UserId };
        var response = await _createBatchReservationHandler.HandleAsync(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations([FromQuery] int userId, CancellationToken cancellationToken)
    {
        var query = new GetUserReservationsQuery(userId);
        var result = await _getUserReservationsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }
}
