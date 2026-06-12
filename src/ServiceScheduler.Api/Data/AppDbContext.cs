using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Models;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AvailabilityWindow> AvailabilityWindows => Set<AvailabilityWindow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceItem>()
            .Property(s => s.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Status)
            .HasConversion<string>();
    }
}
