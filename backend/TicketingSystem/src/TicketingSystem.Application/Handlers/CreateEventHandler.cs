using System;
using System.Threading;
using System.Threading.Tasks;
using TicketingSystem.Application.Commands;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Interfaces;
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
            Status = "Active"
        };

        if (@event.Sectors == null)
        {
            @event.Sectors = new System.Collections.Generic.List<Sector>();
        }

        foreach (var sectorReq in command.Sectors)
        {
            var sector = new Sector
            {
                Name = sectorReq.Name,
                Price = sectorReq.Price,
                Capacity = sectorReq.Seats.Count
            };

            if (sector.Seats == null)
            {
                sector.Seats = new System.Collections.Generic.List<Seat>();
            }

            foreach (var seatReq in sectorReq.Seats)
            {
                var seat = new Seat
                {
                    Id = Guid.NewGuid(),
                    RowIdentifier = seatReq.RowIdentifier,
                    SeatNumber = seatReq.SeatNumber,
                    Status = "Available",
                    Version = 0
                };
                sector.Seats.Add(seat);
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
