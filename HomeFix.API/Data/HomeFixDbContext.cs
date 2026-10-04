using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Data;

public class HomeFixDbContext(DbContextOptions<HomeFixDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<ServiceCategory>(e => e.HasIndex(c => c.Name).IsUnique());

        b.Entity<ProviderProfile>(e =>
        {
            e.Property(p => p.HourlyRate).HasPrecision(10, 2);
            e.Property(p => p.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            // A provider profile belongs to exactly one user (1-to-1)
            e.HasOne(p => p.User).WithOne(u => u.ProviderProfile)
                .HasForeignKey<ProviderProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Category).WithMany(c => c.Providers)
                .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Service>(e =>
        {
            e.Property(s => s.Price).HasPrecision(10, 2);
            e.HasOne(s => s.ProviderProfile).WithMany(p => p.Services)
                .HasForeignKey(s => s.ProviderProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Category).WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Booking>(e =>
        {
            e.Property(x => x.EstimatedPrice).HasPrecision(10, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProviderProfile).WithMany(p => p.Bookings)
                .HasForeignKey(x => x.ProviderProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Review>(e =>
        {
            // Only ONE review per booking (enforced by the database as well)
            e.HasIndex(r => r.BookingId).IsUnique();
            e.HasOne(r => r.Booking).WithOne(x => x.Review)
                .HasForeignKey<Review>(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.ProviderProfile).WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProviderProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Report>(e =>
        {
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.Reporter).WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.ReportedUser).WithMany().HasForeignKey(r => r.ReportedUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Review).WithMany().HasForeignKey(r => r.ReviewId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
