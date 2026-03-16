using Microsoft.EntityFrameworkCore;
using EventPortal.Api.Modules.Auth.Entities;

namespace EventPortal.Api.Modules.Shared.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

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
    }
}
