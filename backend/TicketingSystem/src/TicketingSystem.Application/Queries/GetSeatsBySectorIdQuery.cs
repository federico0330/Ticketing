namespace TicketingSystem.Application.Queries;

public record GetSeatsBySectorIdQuery(int SectorId, int? CurrentUserId = null);