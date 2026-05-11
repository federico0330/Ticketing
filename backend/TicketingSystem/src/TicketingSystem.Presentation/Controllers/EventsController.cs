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
    private readonly IUpdateEventHandler _updateEventHandler;
    private readonly IDeleteEventHandler _deleteEventHandler;

    public EventsController(
        IGetAllEventsHandler getAllEventsHandler,
        IGetSectorsByEventIdHandler getSectorsByEventIdHandler,
        ICreateEventHandler createEventHandler,
        IUpdateEventHandler updateEventHandler,
        IDeleteEventHandler deleteEventHandler)
    {
        _getAllEventsHandler = getAllEventsHandler;
        _getSectorsByEventIdHandler = getSectorsByEventIdHandler;
        _createEventHandler = createEventHandler;
        _updateEventHandler = updateEventHandler;
        _deleteEventHandler = deleteEventHandler;
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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateEventCommand(id, request.Name, request.EventDate, request.Venue);
        var result = await _updateEventHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var command = new DeleteEventCommand(id);
        await _deleteEventHandler.HandleAsync(command, cancellationToken);
        return NoContent();
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
