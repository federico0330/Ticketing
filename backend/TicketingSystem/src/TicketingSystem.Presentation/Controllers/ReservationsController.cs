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

    [HttpPost("{id}/pay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] PaymentRequest request)
    {
        if (id != request.ReservationId)
        {
            return BadRequest(new { Error = "ID_MISMATCH", Message = "El ID de la reserva no coincide." });
        }

        var command = new ConfirmPaymentCommand 
        { 
            ReservationId = request.ReservationId, 
            CardToken = request.CardToken 
        };

        var response = await _confirmPaymentHandler.HandleAsync(command);
        return Ok(response);
    }

    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations([FromQuery] int userId)
    {
        var query = new GetUserReservationsQuery(userId);
        var result = await _getUserReservationsHandler.HandleAsync(query);
        return Ok(result);
    }
}
