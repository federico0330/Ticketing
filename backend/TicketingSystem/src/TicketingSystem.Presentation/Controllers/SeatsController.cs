using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Handlers;
using TicketingSystem.Application.Queries;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Presentation.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class SeatsController : ControllerBase
{
    private readonly GetSeatsBySectorIdHandler _getSeatsBySectorIdHandler;
    private readonly CreateReservationHandler _createReservationHandler;

    public SeatsController(
        GetSeatsBySectorIdHandler getSeatsBySectorIdHandler,
        CreateReservationHandler createReservationHandler)
    {
        _getSeatsBySectorIdHandler = getSeatsBySectorIdHandler;
        _createReservationHandler = createReservationHandler;
    }

    /// Obtiene el estado actual de todos los asientos de un sector.
    [HttpGet("sectors/{sectorId}/seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeatsBySector(int sectorId)
    {
        var seats = await _getSeatsBySectorIdHandler.HandleAsync(new GetSeatsBySectorIdQuery(sectorId));
        return Ok(seats);
    }

    /// Intenta reservar un asiento para un usuario.
    [HttpPost("seats/reservations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "VALIDATION_ERROR", Message = "Datos de entrada inválidos." });

        try
        {
            var command = new CreateReservationCommand(request.SeatId, request.UserId);
            var reservation = await _createReservationHandler.HandleAsync(command);
            return StatusCode(StatusCodes.Status201Created, reservation);
        }
        catch (SeatNotFoundException ex)
        {
            return NotFound(new { Error = "NOT_FOUND", ex.Message });
        }
        catch (SeatNotAvailableException ex)
        {
            // 409 Conflict: el asiento está tomado, no es un error de servidor
            return Conflict(new { Error = "SEAT_NOT_AVAILABLE", ex.Message });
        }
    }
}