using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Application.Queries;

namespace TicketingSystem.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IGetAllEventsHandler _getAllEventsHandler;
    private readonly IGetSectorsByEventIdHandler _getSectorsByEventIdHandler;

    public EventsController(
        IGetAllEventsHandler getAllEventsHandler,
        IGetSectorsByEventIdHandler getSectorsByEventIdHandler)
    {
        _getAllEventsHandler = getAllEventsHandler;
        _getSectorsByEventIdHandler = getSectorsByEventIdHandler;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _getAllEventsHandler.HandleAsync(new GetAllEventsQuery());
        return Ok(events);
    }

    [HttpGet("{id}/sectors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectors(int id)
    {
        var sectors = await _getSectorsByEventIdHandler.HandleAsync(new GetSectorsByEventIdQuery(id));
        return Ok(sectors);
    }
}
