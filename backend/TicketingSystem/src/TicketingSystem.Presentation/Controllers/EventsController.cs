using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
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
    private readonly ICreateEventHandler _createEventHandler;

    public EventsController(
        IGetAllEventsHandler getAllEventsHandler,
        IGetSectorsByEventIdHandler getSectorsByEventIdHandler,
        ICreateEventHandler createEventHandler)
    {
        _getAllEventsHandler = getAllEventsHandler;
        _getSectorsByEventIdHandler = getSectorsByEventIdHandler;
        _createEventHandler = createEventHandler;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand(request.Name, request.EventDate, request.Venue, request.Sectors);
        var result = await _createEventHandler.HandleAsync(command, cancellationToken);
        return StatusCode(201, result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var events = await _getAllEventsHandler.HandleAsync(new GetAllEventsQuery(User.IsInRole("Admin")), cancellationToken);
        return Ok(events);
    }

    [HttpGet("{id}/sectors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectors(int id, CancellationToken cancellationToken)
    {
        var sectors = await _getSectorsByEventIdHandler.HandleAsync(new GetSectorsByEventIdQuery(id), cancellationToken);
        return Ok(sectors);
    }
}
