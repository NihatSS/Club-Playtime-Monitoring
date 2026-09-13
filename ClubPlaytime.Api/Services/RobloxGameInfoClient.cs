using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ClubPlaytime.Api.Options;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed record RobloxGameInfo(
    string Name,
    long? RootPlaceId,
    string? IconUrl,
    string? ThumbnailUrl,
    DateTime CreatedUtc);

/// <summary>
/// Resolves Roblox game metadata (name, icon) for the Recent Activity feed.
/// Icons are resolved from real Roblox endpoints (places→universe API, games
/// API for the name, thumbnails API for the icon) and cached for 24h per
/// place/universe.
/// </summary>
public sealed class RobloxGameInfoClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<MonitoringOptions> options) : IRobloxGameInfoClient
{
    private static readonly Dictionary<string, RobloxGameInfo> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    public async Task<RobloxGameInfo?> GetByPlaceIdAsync(long placeId, CancellationToken cancellationToken = default)
    {
        if (Cache.TryGetValue($"place:{placeId}", out var cached) && DateTime.UtcNow - cached.CreatedUtc < CacheLifetime)
        {
            return cached;
        }

        var info = await FetchAsync(placeId, cancellationToken);
        if (info is null)
        {
            // Serve the stale entry rather than nothing when Roblox hiccups.
            return Cache.TryGetValue($"place:{placeId}", out var stale) ? stale : null;
        }

        Cache[$"place:{placeId}"] = info;
        return info;
    }

    public async Task<RobloxGameInfo?> GetByUniverseIdAsync(long universeId, CancellationToken cancellationToken = default)
    {
        if (Cache.TryGetValue($"universe:{universeId}", out var cached) && DateTime.UtcNow - cached.CreatedUtc < CacheLifetime)
        {
            return cached;
        }

        var info = await FetchByUniverseAsync(universeId, cancellationToken);
        if (info is null)
        {
            return Cache.TryGetValue($"universe:{universeId}", out var stale) ? stale : null;
        }

        Cache[$"universe:{universeId}"] = info;
        return info;
    }

    private async Task<RobloxGameInfo?> FetchAsync(long placeId, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("RobloxGameInfo");

            // 1) Universe ID for the place (public places API).
            var universeResponse = await client.GetFromJsonAsync<UniverseIdResponse>(
                $"https://apis.roblox.com/universes/v1/places/{placeId}/universe",
                cancellationToken);
            var universeId = universeResponse?.UniverseId;
            if (universeId is null)
            {
                return null;
            }

            return await FetchByUniverseAsync(universeId.Value, cancellationToken, placeId, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<RobloxGameInfo?> FetchByUniverseAsync(
        long universeId,
        CancellationToken cancellationToken,
        long? knownPlaceId = null,
        CancellationToken? outerCancellationToken = null)
    {
        try
        {
            var client = httpClientFactory.CreateClient("RobloxGameInfo");

            // 2) Game name + root place from the games API.
            var gamesBatch = await client.GetFromJsonAsync<GamesBatch>(
                $"https://games.roblox.com/v1/games?universeIds={universeId}",
                cancellationToken);
            var game = gamesBatch?.Data?.Count > 0 ? gamesBatch.Data[0] : null;
            var name = game?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            // 3) Square game icon (150px) from the official thumbnails API.
            string? iconUrl = null;
            try
            {
                var icons = await client.GetFromJsonAsync<GameIconBatch>(
                    $"https://thumbnails.roblox.com/v1/games/icons?universeIds={universeId}&size=150x150&format=Png&isCircular=false",
                    cancellationToken);
                iconUrl = icons?.Data?.Count > 0 ? icons.Data[0].ImageUrl : null;
            }
            catch
            {
                // Icon is optional; the name alone still renders a useful row.
            }

            return new RobloxGameInfo(name, game?.RootPlaceId ?? knownPlaceId, iconUrl, null, DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    private sealed class UniverseIdResponse
    {
        [JsonPropertyName("universeId")]
        public long? UniverseId { get; init; }
    }

    private sealed class GamesBatch
    {
        [JsonPropertyName("data")]
        public List<GameEntry>? Data { get; init; }
    }

    private sealed class GameEntry
    {
        [JsonPropertyName("id")]
        public long? Id { get; init; }

        [JsonPropertyName("rootPlaceId")]
        public long? RootPlaceId { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class GameIconBatch
    {
        [JsonPropertyName("data")]
        public List<GameIconEntry>? Data { get; init; }
    }

    private sealed class GameIconEntry
    {
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; init; }
    }
}

public interface IRobloxGameInfoClient
{
    Task<RobloxGameInfo?> GetByPlaceIdAsync(long placeId, CancellationToken cancellationToken = default);
    Task<RobloxGameInfo?> GetByUniverseIdAsync(long universeId, CancellationToken cancellationToken = default);
}
