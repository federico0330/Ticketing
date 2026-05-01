using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Events.AnyAsync()) return;

        for (int i = 1; i <= 5; i++)
        {
            var user = new User
            {
                Name = $"Usuario {i}",
                Email = $"user{i}@ticketing.com",
                PasswordHash = $"user{i}" 
            };
            context.Users.Add(user);
        }
        await context.SaveChangesAsync();

        var concertEvent = new Event
        {
            Name = "Concierto de Rock 2025",
            EventDate = new DateTime(2025, 12, 20, 21, 0, 0, DateTimeKind.Utc),
            Venue = "Estadio Central",
            Status = "Active"
        };
        context.Events.Add(concertEvent);
        await context.SaveChangesAsync();

        var sectorBaja = new Sector
        {
            EventId = concertEvent.Id,
            Name = "Platea Baja",
            Price = 5000.00m,
            Capacity = 50
        };

        var sectorAlta = new Sector
        {
            EventId = concertEvent.Id,
            Name = "Platea Alta",
            Price = 3000.00m,
            Capacity = 50
        };

        context.Sectors.AddRange(sectorBaja, sectorAlta);
        await context.SaveChangesAsync();

        var seatsSectorBaja = GenerateSeats(sectorBaja.Id, new[] { "A", "B", "C", "D", "E" }, 10);
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
