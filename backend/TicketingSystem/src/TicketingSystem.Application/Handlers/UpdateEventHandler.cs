using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class UpdateEventHandler : IUpdateEventHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventDto> HandleAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new EventNotFoundException(command.Id);

        @event.Name = command.Name;
        @event.EventDate = command.EventDate;
        @event.Venue = command.Venue;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _eventRepository.UpdateAsync(@event, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new EventDto(
            @event.Id,
            @event.Name,
            @event.EventDate,
            @event.Venue,
            @event.Status,
            @event.Sectors.Count,
            @event.Sectors.Sum(s => s.Seats.Count)
        );
    }
}
