using ClubPlaytime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Data;

public sealed class ClubPlaytimeDbContext(DbContextOptions<ClubPlaytimeDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<DailyPlaytime> DailyPlaytime => Set<DailyPlaytime>();

    public DbSet<PlayerActivityEvent> PlayerActivityEvents => Set<PlayerActivityEvent>();

    public DbSet<User> Users => Set<User>();

    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();

    public DbSet<Tournament> Tournaments => Set<Tournament>();

    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();

    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();

    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();

    public DbSet<TournamentPrize> TournamentPrizes => Set<TournamentPrize>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(player => player.RobloxUserId).IsUnique();
            entity.Property(player => player.Username).HasMaxLength(100).IsRequired();
            entity.Property(player => player.ProfileUrl).HasMaxLength(300).IsRequired();
            entity.Property(player => player.CurrentlyPlaying).HasMaxLength(200);
            entity.Property(player => player.AvatarUrl).HasMaxLength(700);
            entity.Property(player => player.Club).HasMaxLength(100).HasDefaultValue("PIH");
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
            entity.Property(user => user.DiscordUserId).HasMaxLength(100);
            entity.Property(user => user.RobloxUsername).HasMaxLength(100);

            // A tracker player can be claimed by at most one website account.
            entity.HasIndex(user => user.PlayerId).IsUnique();
            entity.HasOne(user => user.Player)
                .WithMany()
                .HasForeignKey(user => user.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.Property(code => code.Code).HasMaxLength(32).IsRequired();
            entity.HasIndex(code => code.Code).IsUnique();
            entity.HasIndex(code => new { code.RobloxUserId, code.CreatedAt });
            entity.Property(code => code.ClaimToken).HasMaxLength(64);
        });

        modelBuilder.Entity<JoinRequest>(entity =>
        {
            entity.HasIndex(r => new { r.RobloxUserId, r.Status });
            entity.Property(r => r.RobloxUsername).HasMaxLength(100).IsRequired();
            entity.Property(r => r.DiscordUserId).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Club).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Note).HasMaxLength(500);
            entity.Property(r => r.ReviewedBy).HasMaxLength(50);

            // A website account can only have one active (non-declined) request.
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(120).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(4000);
            entity.Property(t => t.PrizeInfo).HasMaxLength(500);
            entity.Property(t => t.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Prevent duplicate registration rows at the database level.
            entity.HasMany(t => t.Participants)
                .WithOne(p => p.Tournament)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.Teams)
                .WithOne(tm => tm.Tournament)
                .HasForeignKey(tm => tm.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.Matches)
                .WithOne(m => m.Tournament)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.Prizes)
                .WithOne(p => p.Tournament)
                .HasForeignKey(p => p.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentParticipant>(entity =>
        {
            // One registration per player per tournament.
            entity.HasIndex(p => new { p.TournamentId, p.PlayerId }).IsUnique();
            entity.HasOne(p => p.Player)
                .WithMany()
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(p => p.Team)
                .WithMany(tm => tm.Members)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(p => p.DisqualifiedReason).HasMaxLength(200);
        });

        modelBuilder.Entity<TournamentTeam>(entity =>
        {
            entity.Property(tm => tm.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(tm => new { tm.TournamentId, tm.Name }).IsUnique();
        });

        modelBuilder.Entity<TournamentMatch>(entity =>
        {
            entity.HasIndex(m => new { m.TournamentId, m.Round, m.Slot }).IsUnique();
            entity.HasOne(m => m.Participant1)
                .WithMany()
                .HasForeignKey(m => m.Participant1Id)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.Participant2)
                .WithMany()
                .HasForeignKey(m => m.Participant2Id)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.Team1)
                .WithMany()
                .HasForeignKey(m => m.Team1Id)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.Team2)
                .WithMany()
                .HasForeignKey(m => m.Team2Id)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.Winner)
                .WithMany()
                .HasForeignKey(m => m.WinnerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.WinnerTeam)
                .WithMany()
                .HasForeignKey(m => m.WinnerTeamId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.NextMatch)
                .WithMany()
                .HasForeignKey(m => m.NextMatchId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(m => m.Note).HasMaxLength(200);
        });

        modelBuilder.Entity<TournamentPrize>(entity =>
        {
            entity.Property(p => p.Description).HasMaxLength(300).IsRequired();
            entity.HasIndex(p => new { p.TournamentId, p.Placement }).IsUnique();
        });


    }
}
