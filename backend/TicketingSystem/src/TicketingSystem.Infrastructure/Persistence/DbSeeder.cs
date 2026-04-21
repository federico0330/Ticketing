using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Solo ejecutar si no hay datos (idempotente)
        if (await context.Events.AnyAsync()) return;

        // Usuario de prueba
        var testUser = new User
        {
            Name = "Usuario Demo",
            Email = "demo@ticketing.com",
            PasswordHash = "hashed_password_demo"
        };
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        // Evento principal
        var concertEvent = new Event
        {
            Name = "Concierto de Rock 2025",
            EventDate = new DateTime(2025, 12, 20, 21, 0, 0, DateTimeKind.Utc),
            Venue = "Estadio Central",
            Status = "Active"
        };
        context.Events.Add(concertEvent);
        await context.SaveChangesAsync();

        // Sector 1: Platea Baja
        var sectorBaja = new Sector
        {
            EventId = concertEvent.Id,
            Name = "Platea Baja",
            Price = 5000.00m,
            Capacity = 50
        };

        // Sector 2: Platea Alta
        var sectorAlta = new Sector
        {
            EventId = concertEvent.Id,
            Name = "Platea Alta",
            Price = 3000.00m,
            Capacity = 50
        };

        context.Sectors.AddRange(sectorBaja, sectorAlta);
        await context.SaveChangesAsync();

        // 50 asientos para Sector 1: Filas A-E, 10 asientos cada una
        var seatsSectorBaja = GenerateSeats(sectorBaja.Id, new[] { "A", "B", "C", "D", "E" }, 10);

        // 50 asientos para Sector 2: Filas F-J, 10 asientos cada una
        var seatsSectorAlta = GenerateSeats(sectorAlta.Id, new[] { "F", "G", "H", "I", "J" }, 10);

        context.Seats.AddRange(seatsSectorBaja);
        context.Seats.AddRange(seatsSectorAlta);
        await context.SaveChangesAsync();
    }

    private static IEnumerable<Seat> GenerateSeats(int sectorId, string[] rows, int seatsPerRow)
    {
        foreach (var row in rows)
        {
            for (int number = 1; number <= seatsPerRow; number++)
            {
                yield return new Seat
                {
                    Id = Guid.NewGuid(),
                    SectorId = sectorId,
                    RowIdentifier = row,
                    SeatNumber = number,
                    Status = "Available",
                    Version = 0
                };
            }
        }
    }
}