using Microsoft.EntityFrameworkCore;
using ReservationService.Models;

namespace ReservationService.Data;

public class ReservationServiceContext : DbContext
{
    public ReservationServiceContext(DbContextOptions<ReservationServiceContext> options) : base(options) { }

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(r => r.Status).HasConversion<string>();
            entity.Property(r => r.Condition).HasConversion<string>();
            entity.Property(r => r.LateFee).HasPrecision(10, 2);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.BookId);
        });

        modelBuilder.Entity<Waitlist>(entity =>
        {
            entity.Property(w => w.Status).HasConversion<string>();
            entity.HasIndex(w => w.BookId);
            entity.HasIndex(w => w.UserId);
        });
    }
}