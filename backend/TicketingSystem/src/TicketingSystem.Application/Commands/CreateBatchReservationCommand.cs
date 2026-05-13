namespace TicketingSystem.Application.Commands;

public class CreateBatchReservationCommand
{
    public List<Guid> SeatIds { get; set; } = new();
    public int UserId { get; set; }
}
