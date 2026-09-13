namespace ClubPlaytime.Api.DTOs;

/// <summary>A single achievement with its unlock state and progress for one player.</summary>
public sealed class AchievementDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public bool Unlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? Source { get; set; }
    public string? Detail { get; set; }

    /// <summary>Backend-formatted progress text, e.g. "12h 30m / 50h" or "5 / 7 days".</summary>
    public string ProgressLabel { get; set; } = string.Empty;

    /// <summary>0-100 completion percentage toward the requirement.</summary>
    public int ProgressPercent { get; set; }
}

/// <summary>All achievements for one player, unlocked first.</summary>
public sealed class PlayerAchievementsDto
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int UnlockedCount { get; set; }
    public int TotalCount { get; set; }
    public List<AchievementDto> Achievements { get; set; } = new();
}

/// <summary>One data point of a playtime chart.</summary>
public sealed record ChartPointDto(string Label, long PlaySeconds);

/// <summary>
/// Detailed statistics for one player, computed from real tracker data
/// (DailyPlaytime rows and Player.TotalPlaySeconds).
/// </summary>
public sealed class PlayerStatsDto
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Club { get; set; } = string.Empty;

    public long TodayPlaySeconds { get; set; }
    public long WeekPlaySeconds { get; set; }
    public long MonthPlaySeconds { get; set; }
    public long TotalPlaySeconds { get; set; }

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int DaysPlayed { get; set; }
    public DateOnly? LastActiveDate { get; set; }

    /// <summary>TotalPlaySeconds / DaysPlayed — average across days with recorded playtime.</summary>
    public long AverageDailyPlaySeconds { get; set; }

    /// <summary>1-based all-time leaderboard position; null when the player has no playtime.</summary>
    public int? TotalRank { get; set; }

    public int AchievementsUnlocked { get; set; }
    public int TotalAchievements { get; set; }

    /// <summary>Last 30 days, one point per day (zero-filled).</summary>
    public List<ChartPointDto> DailyChart { get; set; } = new();

    /// <summary>Last 12 weeks, bucketed by week starting Monday.</summary>
    public List<ChartPointDto> WeeklyChart { get; set; } = new();

    /// <summary>Last 12 months, bucketed by calendar month.</summary>
    public List<ChartPointDto> MonthlyChart { get; set; } = new();
}

/// <summary>
/// Compact progress summary embedded in the player details and profile
/// responses: the numbers shown alongside existing playtime stats.
/// </summary>
public sealed class PlayerProgressSummaryDto
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int DaysPlayed { get; set; }
    public DateOnly? LastActiveDate { get; set; }
    public int AchievementsUnlocked { get; set; }
    public int TotalAchievements { get; set; }
    public int? TotalRank { get; set; }
}
