using System;
using System.Threading;
using System.Threading.Tasks;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Handlers;

public class CreateEventHandler : ICreateEventHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventDto> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = new Event
        {
            Name = command.Name,
            EventDate = command.EventDate,
            Venue = command.Venue,
            Status = EventStatus.Active
        };

        foreach (var sectorReq in command.Sectors)
        {
            // Capacity derivada de los seats explícitos: el admin marcó qué celdas son "activas" en la grilla del front.
            var sector = new Sector
            {
                Name = sectorReq.Name,
                Price = sectorReq.Price,
                Capacity = sectorReq.Seats.Count
            };

            foreach (var seatReq in sectorReq.Seats)
            {
                sector.Seats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    RowIdentifier = seatReq.RowIdentifier,
                    SeatNumber = seatReq.SeatNumber,
                    Status = SeatStatus.Available,
                    Version = 0
                });
            }

            @event.Sectors.Add(sector);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _eventRepository.CreateAsync(@event, cancellationToken);
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
