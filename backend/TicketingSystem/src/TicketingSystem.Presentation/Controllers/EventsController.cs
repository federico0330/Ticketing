using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Handlers;
using TicketingSystem.Application.Queries;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly GetAllEventsHandler _getAllEventsHandler;
    private readonly GetSectorsByEventIdHandler _getSectorsByEventIdHandler;

    public EventsController(
        GetAllEventsHandler getAllEventsHandler,
        GetSectorsByEventIdHandler getSectorsByEventIdHandler)
    {
        _getAllEventsHandler = getAllEventsHandler;
        _getSectorsByEventIdHandler = getSectorsByEventIdHandler;
    }

    /// Obtiene la lista de todos los eventos disponibles.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _getAllEventsHandler.HandleAsync(new GetAllEventsQuery());
        return Ok(events);
    }

    /// Obtiene los sectores de un evento específico.
    [HttpGet("{id}/sectors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectors(int id)
    {
        try
        {
            var sectors = await _getSectorsByEventIdHandler.HandleAsync(new GetSectorsByEventIdQuery(id));
            return Ok(sectors);
        }
        catch (EventNotFoundException ex)
        {
            return NotFound(new { Error = "NOT_FOUND", ex.Message });
        }
    }
}