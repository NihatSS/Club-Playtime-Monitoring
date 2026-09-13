namespace ClubPlaytime.Api.Models;

/// <summary>
/// One row per tracker player: aggregated playtime-streak state and cached
/// play-day counters. Everything here is derived from DailyPlaytime records —
/// never user input — and is recomputed by PlayerProgressService.
/// </summary>
public sealed class PlayerStreak
{
    public int PlayerId { get; set; }

    /// <summary>Consecutive play-days ending at the most recent activity (UTC calendar days).</summary>
    public int CurrentStreak { get; set; }

    /// <summary>Best consecutive play-day run ever recorded for this player.</summary>
    public int LongestStreak { get; set; }

    /// <summary>Number of distinct calendar days with any recorded playtime.</summary>
    public int DaysPlayed { get; set; }

    /// <summary>Most recent day with playtime, null for players with no playtime yet.</summary>
    public DateOnly? LastActiveDate { get; set; }

    /// <summary>When the streak row was last recomputed from DailyPlaytime.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// A single achievement definition. Definitions live in code
/// (AchievementCatalog) so unlocking is always evaluated against real tracker
/// data; the database only stores who unlocked what and when.
/// </summary>
public sealed class PlayerAchievement
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public Player? Player { get; set; }

    /// <summary>Stable identifier from AchievementCatalog (e.g. "first-hour").</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string AchievementKey { get; set; } = string.Empty;

    public DateTime UnlockedAt { get; set; }

    /// <summary>How the achievement was satisfied. Only "Auto" unlocks happen without an admin action.</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public string Source { get; set; } = "Auto";

    /// <summary>Human-readable context captured at unlock time (e.g. tournament name, rank period).</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(300)]
    public string? Detail { get; set; }
}
