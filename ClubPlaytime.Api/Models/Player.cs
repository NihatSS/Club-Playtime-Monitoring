namespace ClubPlaytime.Api.Models;

public sealed class Player
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string ProfileUrl { get; set; } = string.Empty;

    public long RobloxUserId { get; set; }

    public bool IsOnline { get; set; }

    public string? CurrentlyPlaying { get; set; }

    public DateTime? LastSeenPlaying { get; set; }

    public long TotalPlaySeconds { get; set; }

    public string? AvatarUrl { get; set; }

    public string Club { get; set; } = "PIH";

    public string? DiscordUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Last time a website account linked to this player was active on the
    /// site (UTC). Drives the "on the website" presence state.
    /// </summary>
    public DateTime? LastSeenOnSite { get; set; }

    public ICollection<DailyPlaytime> DailyPlaytimes { get; set; } = new List<DailyPlaytime>();

    public ICollection<PlayerActivityEvent> ActivityEvents { get; set; } = new List<PlayerActivityEvent>();
}
