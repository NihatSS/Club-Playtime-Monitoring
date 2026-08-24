using ClubPlaytime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Data;

public sealed class ClubPlaytimeDbContext(DbContextOptions<ClubPlaytimeDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<DailyPlaytime> DailyPlaytime => Set<DailyPlaytime>();

    public DbSet<PlayerActivityEvent> PlayerActivityEvents => Set<PlayerActivityEvent>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(player => player.RobloxUserId).IsUnique();
            entity.Property(player => player.Username).HasMaxLength(100).IsRequired();
            entity.Property(player => player.ProfileUrl).HasMaxLength(300).IsRequired();
            entity.Property(player => player.CurrentlyPlaying).HasMaxLength(200);
            entity.Property(player => player.AvatarUrl).HasMaxLength(700);
            entity.Property(player => player.Club).HasMaxLength(10).HasDefaultValue("PIH");
            entity.Property(player => player.DiscordUserId).HasMaxLength(100);
        });

        modelBuilder.Entity<DailyPlaytime>(entity =>
        {
            entity.ToTable("DailyPlaytime");
            entity.HasIndex(day => new { day.PlayerId, day.Date }).IsUnique();
            entity.HasOne(day => day.Player)
                .WithMany(player => player.DailyPlaytimes)
                .HasForeignKey(day => day.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerActivityEvent>(entity =>
        {
            entity.Property(activity => activity.EventType).HasMaxLength(40).IsRequired();
            entity.Property(activity => activity.Message).HasMaxLength(500).IsRequired();
            entity.HasIndex(activity => new { activity.PlayerId, activity.OccurredAt });
            entity.HasOne(activity => activity.Player)
                .WithMany(player => player.ActivityEvents)
                .HasForeignKey(activity => activity.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.Property(user => user.Username).HasMaxLength(50).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(10).HasDefaultValue("User");
        });

        // Seed default admin user (password: admin123)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
