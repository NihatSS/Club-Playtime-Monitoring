using System.ComponentModel.DataAnnotations;

namespace ClubPlaytime.Api.DTOs;

public sealed class AddPlayerRequest
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long RobloxUserId { get; set; }

    [Required, MaxLength(10)]
    public string Club { get; set; } = "PIH";

    [MaxLength(100)]
    public string? DiscordUserId { get; set; }
}

public sealed class AdjustPlaytimeRequest
{
    public DateOnly? Date { get; set; }

    public long DeltaSeconds { get; set; }

    public string? Reason { get; set; }
}

public sealed class PlayerDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string ProfileUrl { get; set; } = string.Empty;

    public long RobloxUserId { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public string? CurrentGame { get; set; }

    public DateTime? LastSeenPlaying { get; set; }

    public long TodayPlaySeconds { get; set; }

    public long TotalPlaySeconds { get; set; }

    public string? AvatarUrl { get; set; }

    public string Club { get; set; } = "PIH";

    public string? DiscordUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public PlayerDto() { }

    public PlayerDto(int id, string username, string profileUrl, long robloxUserId, string currentStatus, bool isOnline, string? currentGame, DateTime? lastSeenPlaying, long todayPlaySeconds, long totalPlaySeconds, string? avatarUrl, string club, string? discordUserId, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        Username = username;
        ProfileUrl = profileUrl;
        RobloxUserId = robloxUserId;
        CurrentStatus = currentStatus;
        IsOnline = isOnline;
        CurrentGame = currentGame;
        LastSeenPlaying = lastSeenPlaying;
        TodayPlaySeconds = todayPlaySeconds;
        TotalPlaySeconds = totalPlaySeconds;
        AvatarUrl = avatarUrl;
        Club = club;
        DiscordUserId = discordUserId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}

public sealed class DashboardPlayerDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string ProfileUrl { get; set; } = string.Empty;

    public long RobloxUserId { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public string? CurrentGame { get; set; }

    public DateTime? LastSeenPlaying { get; set; }

    public long TodayPlaySeconds { get; set; }

    public long TotalPlaySeconds { get; set; }

    public string? AvatarUrl { get; set; }

    public string Club { get; set; } = "PIH";

    public string? DiscordUserId { get; set; }

    public DashboardPlayerDto() { }

    public DashboardPlayerDto(int id, string username, string profileUrl, long robloxUserId, string currentStatus, string? currentGame, DateTime? lastSeenPlaying, long todayPlaySeconds, long totalPlaySeconds, string? avatarUrl, string club, string? discordUserId)
    {
        Id = id;
        Username = username;
        ProfileUrl = profileUrl;
        RobloxUserId = robloxUserId;
        CurrentStatus = currentStatus;
        CurrentGame = currentGame;
        LastSeenPlaying = lastSeenPlaying;
        TodayPlaySeconds = todayPlaySeconds;
        TotalPlaySeconds = totalPlaySeconds;
        AvatarUrl = avatarUrl;
        Club = club;
        DiscordUserId = discordUserId;
    }
}

public sealed class DailyPlaytimeDto
{
    public DateOnly Date { get; set; }

    public long PlaySeconds { get; set; }

    public DailyPlaytimeDto() { }

    public DailyPlaytimeDto(DateOnly date, long playSeconds)
    {
        Date = date;
        PlaySeconds = playSeconds;
    }
}

public sealed class ActivityEventDto
{
    public int Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public long DeltaSeconds { get; set; }

    public DateTime OccurredAt { get; set; }

    public ActivityEventDto() { }

    public ActivityEventDto(int id, string eventType, string message, long deltaSeconds, DateTime occurredAt)
    {
        Id = id;
        EventType = eventType;
        Message = message;
        DeltaSeconds = deltaSeconds;
        OccurredAt = occurredAt;
    }
}

public sealed class PlayerDetailsDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string ProfileUrl { get; set; } = string.Empty;

    public long RobloxUserId { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public string? CurrentGame { get; set; }

    public DateTime? LastSeenPlaying { get; set; }

    public long TodayPlaySeconds { get; set; }

    public long WeeklyPlaySeconds { get; set; }

    public long MonthlyPlaySeconds { get; set; }

    public long TotalPlaySeconds { get; set; }

    public string? AvatarUrl { get; set; }

    public string Club { get; set; } = "PIH";

    public string? DiscordUserId { get; set; }

    public IReadOnlyList<DailyPlaytimeDto> Last30Days { get; set; } = Array.Empty<DailyPlaytimeDto>();

    public IReadOnlyList<ActivityEventDto> RecentActivity { get; set; } = Array.Empty<ActivityEventDto>();

    public PlayerDetailsDto() { }

    public PlayerDetailsDto(int id, string username, string profileUrl, long robloxUserId, string currentStatus, string? currentGame, DateTime? lastSeenPlaying, long todayPlaySeconds, long weeklyPlaySeconds, long monthlyPlaySeconds, long totalPlaySeconds, string? avatarUrl, string club, string? discordUserId, IReadOnlyList<DailyPlaytimeDto> last30Days, IReadOnlyList<ActivityEventDto> recentActivity)
    {
        Id = id;
        Username = username;
        ProfileUrl = profileUrl;
        RobloxUserId = robloxUserId;
        CurrentStatus = currentStatus;
        CurrentGame = currentGame;
        LastSeenPlaying = lastSeenPlaying;
        TodayPlaySeconds = todayPlaySeconds;
        WeeklyPlaySeconds = weeklyPlaySeconds;
        MonthlyPlaySeconds = monthlyPlaySeconds;
        TotalPlaySeconds = totalPlaySeconds;
        AvatarUrl = avatarUrl;
        Club = club;
        DiscordUserId = discordUserId;
        Last30Days = last30Days;
        RecentActivity = recentActivity;
    }
}

public sealed class LinkDiscordRequest
{
    [Required, MaxLength(100)]
    public string RobloxUsername { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DiscordUserId { get; set; } = string.Empty;
}

public sealed class UpdateClubRequest
{
    [Required, MaxLength(10)]
    public string Club { get; set; } = string.Empty;
}

public sealed class LeaderboardPlayerDto
{
    public int PlayerId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public long PlaySeconds { get; set; }

    public long TotalPlaySeconds { get; set; }

    public string? DiscordUserId { get; set; }

    public LeaderboardPlayerDto() { }

    public LeaderboardPlayerDto(int playerId, string username, string? avatarUrl, long playSeconds, long totalPlaySeconds, string? discordUserId = null)
    {
        PlayerId = playerId;
        Username = username;
        AvatarUrl = avatarUrl;
        PlaySeconds = playSeconds;
        TotalPlaySeconds = totalPlaySeconds;
        DiscordUserId = discordUserId;
    }
}

public sealed class WeeklyLeaderboardDto
{
    public int PlayerId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public long WeeklyPlaySeconds { get; set; }

    public long TotalPlaySeconds { get; set; }

    public WeeklyLeaderboardDto() { }

    public WeeklyLeaderboardDto(int playerId, string username, string? avatarUrl, long weeklyPlaySeconds, long totalPlaySeconds)
    {
        PlayerId = playerId;
        Username = username;
        AvatarUrl = avatarUrl;
        WeeklyPlaySeconds = weeklyPlaySeconds;
        TotalPlaySeconds = totalPlaySeconds;
    }
}
