using System.Text.Json;
using ClubPlaytime.Api.Options;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed class RobloxAvatarClient(
    HttpClient httpClient,
    IOptionsMonitor<MonitoringOptions> options,
    ILogger<RobloxAvatarClient> logger) : IRobloxAvatarClient
{
    public async Task<string?> GetAvatarUrlAsync(long robloxUserId, CancellationToken cancellationToken = default)
    {
        var settings = options.CurrentValue;
        var url = string.Format(settings.AvatarThumbnailUrl, robloxUserId);

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Roblox thumbnail API returned {StatusCode} for user {RobloxUserId}", response.StatusCode, robloxUserId);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var data = document.RootElement.GetProperty("data");
            if (data.GetArrayLength() == 0)
            {
                return null;
            }

            var first = data[0];
            if (first.TryGetProperty("imageUrl", out var imageUrl) && imageUrl.ValueKind == JsonValueKind.String)
            {
                return imageUrl.GetString();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not fetch avatar thumbnail for Roblox user {RobloxUserId}", robloxUserId);
        }

        return null;
    }
}
