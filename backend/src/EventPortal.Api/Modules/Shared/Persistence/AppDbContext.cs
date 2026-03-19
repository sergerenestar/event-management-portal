using Microsoft.EntityFrameworkCore;
using EventPortal.Api.Modules.Auth.Entities;
using EventPortal.Api.Modules.Events.Entities;
using EventPortal.Api.Modules.Registrations.Entities;

namespace EventPortal.Api.Modules.Shared.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<DailyRegistrationSnapshot> DailyRegistrationSnapshots => Set<DailyRegistrationSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.ToTable("AdminUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            e.Property(x => x.IdentityProvider).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalObjectId).HasMaxLength(256).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.LastLoginAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.AdminUserId);
            e.Property(x => x.ExpiresAt).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.IsRevoked).IsRequired().HasDefaultValue(false);
            e.HasOne(x => x.AdminUser)
             .WithMany()
             .HasForeignKey(x => x.AdminUserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalEventbriteId).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.ExternalEventbriteId).IsUnique();
            e.Property(x => x.Name).HasMaxLength(512).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(256);
            e.Property(x => x.StartDate).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.EndDate).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.Venue).HasMaxLength(512);
            e.Property(x => x.Status).HasMaxLength(64);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1024);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();
        });

        modelBuilder.Entity<TicketType>(e =>
        {
            e.ToTable("TicketTypes");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalTicketClassId).HasMaxLength(128).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.Currency).HasMaxLength(8);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();
            e.HasOne(x => x.Event)
             .WithMany(x => x.TicketTypes)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Registration>(e =>
        {
            e.ToTable("Registrations");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalOrderId).HasMaxLength(128);
            e.Property(x => x.ExternalAttendeeId).HasMaxLength(128);
            e.Property(x => x.AttendeeName).HasMaxLength(256);
            e.Property(x => x.AttendeeEmail).HasMaxLength(256);
            e.Property(x => x.RegisteredAt).HasColumnType("datetime2").IsRequired();
            e.Property(x => x.CheckInStatus).HasMaxLength(64);
            e.Property(x => x.SourceSystem).HasMaxLength(64);
            e.HasIndex(x => x.EventId);
            e.HasOne(x => x.Event)
             .WithMany(x => x.Registrations)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TicketType)
             .WithMany(x => x.Registrations)
             .HasForeignKey(x => x.TicketTypeId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DailyRegistrationSnapshot>(e =>
        {
            e.ToTable("DailyRegistrationSnapshots");
            e.HasKey(x => x.Id);
            e.Property(x => x.SnapshotDate).HasColumnType("date").IsRequired();
            e.HasIndex(x => new { x.EventId, x.SnapshotDate });
            e.HasOne(x => x.Event)
             .WithMany()
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TicketType)
             .WithMany()
             .HasForeignKey(x => x.TicketTypeId)
             .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
