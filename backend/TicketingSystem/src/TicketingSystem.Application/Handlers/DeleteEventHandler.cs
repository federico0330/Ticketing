using TicketingSystem.Application.Commands;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Application.Handlers;

public class DeleteEventHandler : IDeleteEventHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEventHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new EventNotFoundException(command.Id);

        if (@event.Status == "Deleted")
            return;

        // Soft delete: conservamos el evento para no romper reservas históricas que lo referencian.
        @event.Status = "Deleted";

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
    }
}
