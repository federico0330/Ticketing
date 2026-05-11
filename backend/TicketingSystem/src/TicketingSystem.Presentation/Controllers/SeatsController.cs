using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Presentation.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class SeatsController : ControllerBase
{
    private readonly IGetSeatsBySectorIdHandler _getSeatsBySectorIdHandler;
    private readonly ICreateReservationHandler _createReservationHandler;

    public SeatsController(
        IGetSeatsBySectorIdHandler getSeatsBySectorIdHandler,
        ICreateReservationHandler createReservationHandler)
    {
        _getSeatsBySectorIdHandler = getSeatsBySectorIdHandler;
        _createReservationHandler = createReservationHandler;
    }

    [HttpGet("sectors/{sectorId}/seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeatsBySector(int sectorId, CancellationToken cancellationToken)
    {
        var seats = await _getSeatsBySectorIdHandler.HandleAsync(new GetSeatsBySectorIdQuery(sectorId), cancellationToken);
        return Ok(seats);
    }

    [HttpPost("seats/reservations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "VALIDATION_ERROR", Message = "Datos de entrada inválidos." });

        var command = new CreateReservationCommand(request.SeatId, request.UserId);
        var reservation = await _createReservationHandler.HandleAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, reservation);
    }
}
