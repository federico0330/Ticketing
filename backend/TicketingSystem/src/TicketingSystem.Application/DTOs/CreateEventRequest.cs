using System;
using System.Collections.Generic;

namespace TicketingSystem.Application.DTOs;

public record CreateEventRequest(string Name, DateTime EventDate, string Venue, List<CreateSectorRequest> Sectors);
