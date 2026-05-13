namespace TicketingSystem.Application.DTOs;

public class BatchReservationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ReservationDto> Reservations { get; set; } = new();
}
