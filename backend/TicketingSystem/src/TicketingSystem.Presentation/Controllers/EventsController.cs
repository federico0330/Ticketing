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

    /// <summary>Obtiene la lista de todos los eventos disponibles.</summary>
    /// <response code="200">Lista de eventos retornada exitosamente.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var events = (await _getAllEventsHandler.HandleAsync(new GetAllEventsQuery()))
                     .Skip((page - 1) * pageSize)
                     .Take(pageSize);
        return Ok(events);
    }

    /// <summary>Obtiene los sectores de un evento específico.</summary>
    /// <param name="id">ID del evento.</param>
    /// <response code="200">Lista de sectores retornada exitosamente.</response>
    /// <response code="404">El evento no fue encontrado.</response>
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