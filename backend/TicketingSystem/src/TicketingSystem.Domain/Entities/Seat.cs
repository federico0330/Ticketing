namespace TicketingSystem.Domain.Entities;

public class Seat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SectorId { get; set; }
    public string RowIdentifier { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string Status { get; set; } = "Available";

    // Token de concurrencia optimista: si dos requests pisan la misma butaca, EF detecta el conflicto y lanza 409.
    public int Version { get; set; } = 0;

    public Sector Sector { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
