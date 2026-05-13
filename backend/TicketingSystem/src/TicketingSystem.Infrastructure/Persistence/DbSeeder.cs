using Microsoft.EntityFrameworkCore;
using TicketingSystem.Application.Security;
using TicketingSystem.Domain.Constants;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var usersToSeed = new[]
        {
            ("Administrador", "admin@ticketing.com",  "admin"),
            ("Usuario 1",     "user1@ticketing.com",  "user1"),
            ("Usuario 2",     "user2@ticketing.com",  "user2"),
            ("Usuario 3",     "user3@ticketing.com",  "user3"),
            ("Usuario 4",     "user4@ticketing.com",  "user4"),
            ("Usuario 5",     "user5@ticketing.com",  "user5"),
        };
        foreach (var (name, email, password) in usersToSeed)
        {
            if (!await context.Users.AnyAsync(u => u.Email == email))
            {
                context.Users.Add(new User { Name = name, Email = email, PasswordHash = PasswordHasher.Hash(password) });
            }
        }
        await context.SaveChangesAsync();

        if (await context.Events.AnyAsync()) return;

        var concertEvent = new Event
        {
            Name = "Concierto de Rock 2025",
            EventDate = new DateTime(2025, 12, 20, 21, 0, 0, DateTimeKind.Utc),
            Venue = "Estadio Central",
            Status = EventStatus.Active
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
                    Status = SeatStatus.Available,
                    Version = 0
                };
            }
        }
    }
}
