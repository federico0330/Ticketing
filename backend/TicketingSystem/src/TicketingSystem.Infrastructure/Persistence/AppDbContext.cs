using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(builder =>
        {
            builder.ToTable("EVENT");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Name).IsRequired();
            builder.Property(e => e.EventDate).IsRequired();
            builder.Property(e => e.Venue).IsRequired();
            builder.Property(e => e.Status).IsRequired().HasDefaultValue("Active");
        });

        modelBuilder.Entity<Sector>(builder =>
        {
            builder.ToTable("SECTOR");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired();
            builder.Property(s => s.Price).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(s => s.Capacity).IsRequired();
            builder.HasOne(s => s.Event)
                   .WithMany(e => e.Sectors)
                   .HasForeignKey(s => s.EventId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Seat>(builder =>
        {
            builder.ToTable("SEAT");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever(); 
            builder.Property(s => s.RowIdentifier).IsRequired();
            builder.Property(s => s.SeatNumber).IsRequired();
            builder.Property(s => s.Status).IsRequired().HasDefaultValue("Available");
            
            // Habilita la verificación de concurrencia para prevenir condiciones de carrera al actualizar asientos
            builder.Property(s => s.Version).IsRequired().HasDefaultValue(0)
                   .IsConcurrencyToken(); 
                   
            builder.HasOne(s => s.Sector)
                   .WithMany(sec => sec.Seats)
                   .HasForeignKey(s => s.SectorId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("USER");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).IsRequired();
            builder.Property(u => u.Email).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Reservation>(builder =>
        {
            builder.ToTable("RESERVATION");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedNever();
            builder.Property(r => r.Status).IsRequired().HasDefaultValue("Pending");
            builder.Property(r => r.ReservedAt).IsRequired();
            builder.Property(r => r.ExpiresAt).IsRequired();
            builder.HasOne(r => r.User)
                   .WithMany(u => u.Reservations)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.Seat)
                   .WithMany(s => s.Reservations)
                   .HasForeignKey(r => r.SeatId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("AUDIT_LOG");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();
            builder.Property(a => a.Action).IsRequired();
            builder.Property(a => a.EntityType).IsRequired();
            builder.Property(a => a.EntityId).IsRequired();
            builder.Property(a => a.Details).IsRequired();
            builder.Property(a => a.CreatedAt).IsRequired();
            builder.HasOne(a => a.User)
                   .WithMany()
                   .HasForeignKey(a => a.UserId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
