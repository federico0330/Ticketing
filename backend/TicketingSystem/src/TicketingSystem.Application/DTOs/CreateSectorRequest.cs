using System.Collections.Generic;

namespace TicketingSystem.Application.DTOs;

public record CreateSectorRequest(string Name, decimal Price, List<ActiveSeatDto> Seats);
