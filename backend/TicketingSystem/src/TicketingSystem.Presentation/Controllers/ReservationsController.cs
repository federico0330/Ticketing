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
    private readonly IGetUserReservationsHandler _getUserReservationsHandler;

    public ReservationsController(
        IConfirmPaymentHandler confirmPaymentHandler,
        IGetUserReservationsHandler getUserReservationsHandler)
    {
        _confirmPaymentHandler = confirmPaymentHandler;
        _getUserReservationsHandler = getUserReservationsHandler;
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

    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations([FromQuery] int userId, CancellationToken cancellationToken)
    {
        var query = new GetUserReservationsQuery(userId);
        var result = await _getUserReservationsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }
}
