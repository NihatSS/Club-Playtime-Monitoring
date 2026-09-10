using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ClubPlaytime.DiscordBot.Services;

public sealed class PlaytimeApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<PlaytimeApiClient> logger)
{
    /// <summary>
    /// Returns true if the HttpClient has a valid BaseAddress configured.
    /// </summary>
    public bool IsConfigured => httpClient.BaseAddress is not null;

    /// <summary>
    /// The effective base URL the client actually uses, or null when unconfigured.
    /// </summary>
    public Uri? EffectiveBaseUrl => httpClient.BaseAddress;

    public async Task<List<PlayerDto>?> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<PlayerDto>>("players", cancellationToken);
    }

    public async Task<PlayerDetailsDto?> GetPlayerDetailsAsync(int playerId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PlayerDetailsDto>($"players/{playerId}", cancellationToken);
    }

    public async Task<PlayerDetailsDto?> GetPlayerByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PlayerDetailsDto>($"players/by-discord/{discordUserId}", cancellationToken);
    }

    public async Task<List<LeaderboardPlayerDto>?> GetLeaderboardAsync(string period, CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<LeaderboardPlayerDto>>(
            $"dashboard/leaderboard?period={period}", cancellationToken);
    }

    /// <summary>
    /// GETs a JSON resource from the API. Returns null when the API answers 404 (resource
    /// genuinely not found), and throws <see cref="ApiUnavailableException"/> when the API
    /// cannot be reached or answers with an error, so callers can distinguish a real
    /// "not found" from an outage instead of showing a misleading message.
    /// </summary>
    /// <remarks>
    /// Transient failures (connection refused/reset, timeout, 5xx) are retried with
    /// exponential backoff. The production API occasionally flaps (brief cold starts /
    /// restarts), which previously made /playtime and /leaderboard fail randomly even
    /// though the user was in the database. A short retry turns those transient blips
    /// into successes without masking a real 404.
    /// </remarks>
    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken) where T : class
    {
        const int maxAttempts = 3;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<T>(path, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Genuine "not found" — never retry, return null so callers show "not in tracker".
                return null;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                lastException = ex;
                var isTransient = ex is HttpRequestException
                    || ex is TaskCanceledException; // request timeout

                if (!isTransient || attempt == maxAttempts)
                {
                    break;
                }

                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                logger.LogWarning(ex,
                    "Transient failure fetching {Path} from {BaseUrl} (attempt {Attempt}/{MaxAttempts}); retrying in {DelayMs}ms",
                    path, httpClient.BaseAddress, attempt, maxAttempts, (int)delay.TotalMilliseconds);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        logger.LogError(lastException, "Failed to fetch {Path} from {BaseUrl} after {Attempts} attempts",
            path, httpClient.BaseAddress, maxAttempts);
        throw new ApiUnavailableException(
            $"The playtime tracker API at {httpClient.BaseAddress} could not be reached.",
            lastException ?? new HttpRequestException("API request failed."));
    }
}

/// <summary>
/// Thrown when the ClubPlaytime API cannot be reached (connection failure, timeout,
/// or a non-404 error response), as opposed to a genuine 404 "not found".
/// </summary>
public sealed class ApiUnavailableException(string message, Exception innerException) : Exception(message, innerException);

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


