using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed class RobloxPresenceClient(
    HttpClient httpClient,
    IOptionsMonitor<MonitoringOptions> options,
    ILogger<RobloxPresenceClient> logger) : IRobloxPresenceClient
{
    private static readonly Uri PresenceApiUri = new("https://presence.roblox.com/v1/presence/users");

    // Note: We use the Roblox Presence API instead of HTML scraping because Roblox
    // renders presence information dynamically via JavaScript. A static HTTP request
    // (e.g., AngleSharp) cannot see the presence element, and even a headless browser
    // won't show it without being logged in as a friend. The Presence API is the
    // only reliable source for online status and current game information.

    public async Task<RobloxPresenceResult> GetPresenceAsync(Player player, CancellationToken cancellationToken = default)
    {
        var results = await GetPresenceBatchAsync([player], cancellationToken);
        return results.TryGetValue(player.RobloxUserId, out var result)
            ? result
            : RobloxPresenceResult.Failed("User not found in presence response.");
    }

    public async Task<IReadOnlyDictionary<long, RobloxPresenceResult>> GetPresenceBatchAsync(
        IReadOnlyList<Player> players,
        CancellationToken cancellationToken = default)
    {
        if (players.Count == 0)
        {
            return new Dictionary<long, RobloxPresenceResult>();
        }

        var settings = options.CurrentValue;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, settings.RequestTimeoutSeconds)));

        try
        {
            var userIds = players.Select(p => p.RobloxUserId).Distinct().ToArray();
            var requestBody = new PresenceRequest { UserIds = userIds };

            using var request = new HttpRequestMessage(HttpMethod.Post, PresenceApiUri);
            request.Content = JsonContent.Create(requestBody);

            // Add .ROBLOSECURITY cookie if configured, for authenticated game name lookups
            if (!string.IsNullOrWhiteSpace(settings.RobloxSecurityCookie))
            {
                request.Headers.Add("Cookie", $".ROBLOSECURITY={settings.RobloxSecurityCookie}");
            }

            using var response = await httpClient.SendAsync(request, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode == 429)
                {
                    logger.LogWarning("Roblox Presence API rate-limited us (429). Consider increasing CheckIntervalSeconds.");
                }
                else if (statusCode == 403)
                {
                    logger.LogWarning("Roblox Presence API returned 403 (Forbidden). The API may require authentication.");
                }

                return players.ToDictionary(
                    p => p.RobloxUserId,
                    p => RobloxPresenceResult.Failed(
                        $"Presence API returned {statusCode} {response.ReasonPhrase}"));
            }

            var result = await response.Content.ReadFromJsonAsync<PresenceResponse>(
                cancellationToken: timeoutCts.Token);

            var presenceByUserId = result?.UserPresences
                ?.ToDictionary(p => p.UserId, p => p)
                ?? new Dictionary<long, PresenceEntry>();

            var targetGame = settings.TargetGameName;

            return players.ToDictionary(
                p => p.RobloxUserId,
                p =>
                {
                    if (!presenceByUserId.TryGetValue(p.RobloxUserId, out var entry))
                    {
                        return RobloxPresenceResult.Failed("User not found in presence response.");
                    }

                    // userPresenceType: 0=Offline, 1=Online, 2=InGame, 3=InStudio, 4=Invisible
                    var isOnline = entry.UserPresenceType != 0 && entry.UserPresenceType != 4;
                    var currentGame = entry.LastLocation;

                    // UserPresenceType 2 = InGame. Check if lastLocation contains the target game name.
                    // Note: Without a .ROBLOSECURITY cookie, lastLocation may be empty even for in-game users.
                    var isPlayingTargetGame = entry.UserPresenceType == 2 &&
                        !string.IsNullOrWhiteSpace(currentGame) &&
                        currentGame.Contains(targetGame, StringComparison.OrdinalIgnoreCase);

                    return new RobloxPresenceResult(true, true, isOnline, isPlayingTargetGame, currentGame, null);
                });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return players.ToDictionary(
                p => p.RobloxUserId,
                p => RobloxPresenceResult.Failed("Roblox presence request timed out."));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error querying Roblox presence for {PlayerCount} players", players.Count);
            return players.ToDictionary(
                p => p.RobloxUserId,
                p => RobloxPresenceResult.Failed($"HTTP error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not parse Roblox presence for {PlayerCount} players", players.Count);
            return players.ToDictionary(
                p => p.RobloxUserId,
                p => RobloxPresenceResult.Failed(ex.Message));
        }
    }

    private sealed class PresenceRequest
    {
        [JsonPropertyName("userIds")]
        public required long[] UserIds { get; init; }
    }

    private sealed class PresenceResponse
    {
        [JsonPropertyName("userPresences")]
        public PresenceEntry[]? UserPresences { get; init; }
    }

    private sealed class PresenceEntry
    {
        [JsonPropertyName("userId")]
        public long UserId { get; init; }

        [JsonPropertyName("userPresenceType")]
        public int UserPresenceType { get; init; }

        [JsonPropertyName("lastLocation")]
        public string? LastLocation { get; init; }

        [JsonPropertyName("placeId")]
        public long? PlaceId { get; init; }

        [JsonPropertyName("lastOnline")]
        public string? LastOnline { get; init; }
    }
}

