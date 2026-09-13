namespace ClubPlaytime.Api.Services;

/// <summary>Achievement definition. Requirements are evaluated against real tracker data only.</summary>
public sealed record AchievementDefinition(
    string Key,
    string Name,
    string Description,
    string Icon,
    string Tier,
    string Unit,
    long TargetValue);

/// <summary>
/// The fixed catalog of achievements. Keys are stable identifiers stored in the
/// PlayerAchievements table — never rename a key, only its display fields.
/// </summary>
public static class AchievementCatalog
{
    public const string TournamentWinnerKey = "tournament-winner";
    public const string Top3PlayerKey = "top-3-player";

    public static readonly IReadOnlyList<AchievementDefinition> All = new AchievementDefinition[]
    {
        new("first-hour", "First Hour", "Reach 1 hour of total playtime.", "⏱️", "time", "seconds", 3_600),
        new("10-hour-club", "10 Hour Club", "Reach 10 hours of total playtime.", "🕐", "time", "seconds", 36_000),
        new("50-hour-club", "50 Hour Club", "Reach 50 hours of total playtime.", "🕔", "time", "seconds", 180_000),
        new("100-hour-club", "100 Hour Club", "Reach 100 hours of total playtime.", "💯", "time", "seconds", 360_000),
        new("500-hour-club", "500 Hour Club", "Reach 500 hours of total playtime.", "🏅", "time", "seconds", 1_800_000),
        new("1000-hour-legend", "1000 Hour Legend", "Reach 1,000 hours of total playtime.", "👑", "time", "seconds", 3_600_000),
        new("7-day-streak", "7 Day Streak", "Record playtime on 7 consecutive days.", "🔥", "streak", "days", 7),
        new("30-day-streak", "30 Day Streak", "Record playtime on 30 consecutive days.", "☄️", "streak", "days", 30),
        new(TournamentWinnerKey, "Tournament Winner", "Win a tracker tournament.", "🥇", "tournament", "count", 1),
        new(Top3PlayerKey, "Top 3 Player", "Reach the top 3 of the all-time leaderboard.", "⭐", "rank", "rank", 3)
    };
}
