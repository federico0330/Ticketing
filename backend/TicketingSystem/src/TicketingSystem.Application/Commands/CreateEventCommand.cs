using System;
using System.Collections.Generic;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands;

public record CreateEventCommand(string Name, DateTime EventDate, string Venue, List<CreateSectorRequest> Sectors);
