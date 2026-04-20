namespace TicketingSystem.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Navegación
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
