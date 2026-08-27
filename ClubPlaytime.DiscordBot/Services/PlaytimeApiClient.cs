using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ClubPlaytime.DiscordBot.Services;

public sealed class PlaytimeApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<PlaytimeApiClient> logger)
{
    /// <summary>
    /// Returns true if the HttpClient has a valid BaseAddress configured.
    /// </summary>
    public bool IsConfigured => httpClient.BaseAddress is not null;

    public async Task<List<PlayerDto>?> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<PlayerDto>>("players", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch players from {BaseUrl}players", httpClient.BaseAddress);
            return null;
        }
    }

    public async Task<PlayerDetailsDto?> GetPlayerDetailsAsync(int playerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PlayerDetailsDto>($"players/{playerId}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch player details for {PlayerId}.", playerId);
            return null;
        }
    }

    public async Task<PlayerDetailsDto?> GetPlayerByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PlayerDetailsDto>($"players/by-discord/{discordUserId}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch player by Discord ID {DiscordId}.", discordUserId);
            return null;
        }
    }

    public async Task<List<LeaderboardPlayerDto>?> GetLeaderboardAsync(string period, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<LeaderboardPlayerDto>>(
                $"dashboard/leaderboard?period={period}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch leaderboard for period '{Period}'.", period);
            return null;
        }
    }
}

public sealed class PlayerDto
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    [JsonPropertyName("profileUrl")] public string ProfileUrl { get; set; } = string.Empty;
    [JsonPropertyName("robloxUserId")] public long RobloxUserId { get; set; }
    [JsonPropertyName("currentStatus")] public string CurrentStatus { get; set; } = string.Empty;
    [JsonPropertyName("isOnline")] public bool IsOnline { get; set; }
    [JsonPropertyName("currentGame")] public string? CurrentGame { get; set; }
    [JsonPropertyName("lastSeenPlaying")] public DateTime? LastSeenPlaying { get; set; }
    [JsonPropertyName("todayPlaySeconds")] public long TodayPlaySeconds { get; set; }
    [JsonPropertyName("totalPlaySeconds")] public long TotalPlaySeconds { get; set; }
    [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
    [JsonPropertyName("club")] public string Club { get; set; } = string.Empty;
    [JsonPropertyName("discordUserId")] public string? DiscordUserId { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }
}

public sealed class PlayerDetailsDto
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    [JsonPropertyName("profileUrl")] public string ProfileUrl { get; set; } = string.Empty;
    [JsonPropertyName("robloxUserId")] public long RobloxUserId { get; set; }
    [JsonPropertyName("currentStatus")] public string CurrentStatus { get; set; } = string.Empty;
    [JsonPropertyName("currentGame")] public string? CurrentGame { get; set; }
    [JsonPropertyName("lastSeenPlaying")] public DateTime? LastSeenPlaying { get; set; }
    [JsonPropertyName("todayPlaySeconds")] public long TodayPlaySeconds { get; set; }
    [JsonPropertyName("weeklyPlaySeconds")] public long WeeklyPlaySeconds { get; set; }
    [JsonPropertyName("monthlyPlaySeconds")] public long MonthlyPlaySeconds { get; set; }
    [JsonPropertyName("totalPlaySeconds")] public long TotalPlaySeconds { get; set; }
    [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
    [JsonPropertyName("club")] public string Club { get; set; } = string.Empty;
    [JsonPropertyName("discordUserId")] public string? DiscordUserId { get; set; }
}

public sealed class LeaderboardPlayerDto
{
    [JsonPropertyName("playerId")] public int PlayerId { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
    [JsonPropertyName("playSeconds")] public long PlaySeconds { get; set; }
    [JsonPropertyName("totalPlaySeconds")] public long TotalPlaySeconds { get; set; }
    [JsonPropertyName("discordUserId")] public string? DiscordUserId { get; set; }
}


