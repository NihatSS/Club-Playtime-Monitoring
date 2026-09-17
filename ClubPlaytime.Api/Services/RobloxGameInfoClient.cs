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
public sealed class RobloxGameInfoClient(IHttpClientFactory httpClientFactory) : IRobloxGameInfoClient
{
    // ConcurrentDictionary: the old Dictionary was mutated from concurrent detail-
    // panel requests, which corrupts buckets and can throw, 500ing unrelated calls.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (RobloxGameInfo Info, DateTime CachedAt)> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan NegativeCacheLifetime = TimeSpan.FromMinutes(10);

    // In-flight dedupe: many concurrent detail-panel opens for the same game fire
    // ONE Roblox call instead of N (previously up to 3 sequential HTTP calls each).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<RobloxGameInfo?>>> InFlight = new(StringComparer.OrdinalIgnoreCase);

    public Task<RobloxGameInfo?> GetByPlaceIdAsync(long placeId, CancellationToken cancellationToken = default) =>
        GetAsync($"place:{placeId}", () => FetchAsync(placeId), cancellationToken);

    public Task<RobloxGameInfo?> GetByUniverseIdAsync(long universeId, CancellationToken cancellationToken = default) =>
        GetAsync($"universe:{universeId}", () => FetchByUniverseAsync(universeId), cancellationToken);

    private static async Task<RobloxGameInfo?> GetAsync(string key, Func<Task<RobloxGameInfo?>> fetch, CancellationToken cancellationToken)
    {
        if (Cache.TryGetValue(key, out var cached))
        {
            var age = DateTime.UtcNow - cached.CachedAt;
            if (age < CacheLifetime || (cached.Info is null && age < NegativeCacheLifetime))
            {
                // Fresh hit, or a recent negative lookup (Roblox hiccup) — don't re-fetch.
                if (cached.Info is not null || age < NegativeCacheLifetime)
                {
                    return cached.Info;
                }
            }
        }

        try
        {
            var lazy = InFlight.GetOrAdd(key, k => new Lazy<Task<RobloxGameInfo?>>(fetch, LazyThreadSafetyMode.ExecutionAndPublication));
            var info = await lazy.Value.WaitAsync(cancellationToken);
            return info
                ?? (Cache.TryGetValue(key, out var stale) ? stale.Info : null); // serve stale over nothing
        }
        finally
        {
            // Only remove if this fetch is still the registered one (avoid clobbering a newer retry).
            if (InFlight.TryGetValue(key, out var done) && done.IsValueCreated && done.Value.IsCompleted)
            {
                InFlight.TryRemove(new KeyValuePair<string, Lazy<Task<RobloxGameInfo?>>>(key, done));
            }
        }
    }

    private static void Store(string key, RobloxGameInfo? info)
    {
        Cache[key] = (info, DateTime.UtcNow);
    }

    private async Task<RobloxGameInfo?> FetchAsync(long placeId)
    {
        try
        {
            var client = httpClientFactory.CreateClient("RobloxGameInfo");

            // 1) Universe ID for the place (public places API).
            var universeResponse = await client.GetFromJsonAsync<UniverseIdResponse>(
                $"https://apis.roblox.com/universes/v1/places/{placeId}/universe");
            var universeId = universeResponse?.UniverseId;
            if (universeId is null)
            {
                Store($"place:{placeId}", null);
                return null;
            }

            return await FetchByUniverseAsync(universeId.Value, placeId);
        }
        catch
        {
            return null;
        }
    }

    private async Task<RobloxGameInfo?> FetchByUniverseAsync(long universeId, long? knownPlaceId = null)
    {
        try
        {
            var client = httpClientFactory.CreateClient("RobloxGameInfo");

            // 2) Game name + root place from the games API.
            var gamesBatch = await client.GetFromJsonAsync<GamesBatch>(
                $"https://games.roblox.com/v1/games?universeIds={universeId}");
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
                    $"https://thumbnails.roblox.com/v1/games/icons?universeIds={universeId}&size=150x150&format=Png&isCircular=false");
                iconUrl = icons?.Data?.Count > 0 ? icons.Data[0].ImageUrl : null;
            }
            catch
            {
                // Icon is optional; the name alone still renders a useful row.
            }

            var info = new RobloxGameInfo(name, game?.RootPlaceId ?? knownPlaceId, iconUrl, null, DateTime.UtcNow);
            if (knownPlaceId is not null)
            {
                Store($"place:{knownPlaceId.Value}", info);
            }
            Store($"universe:{universeId}", info);
            return info;
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
