using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ClubPlaytime.Api.Services;

public sealed class RobloxProfileClient(HttpClient httpClient, ILogger<RobloxProfileClient> logger) : IRobloxProfileClient
{
    // The official public Roblox users API — no authentication required.
    private const string UsersApiBase = "https://users.roblox.com/v1/users";

    public async Task<string?> GetDescriptionAsync(long robloxUserId, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 4;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync($"{UsersApiBase}/{robloxUserId}", cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<RobloxUserDto>(cancellationToken);
                    return user?.Description ?? string.Empty;
                }

                lastException = new HttpRequestException(
                    $"Roblox users API returned {(int)response.StatusCode} for user {robloxUserId}.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        logger.LogWarning(lastException, "Could not fetch Roblox profile description for user {RobloxUserId} after {Attempts} attempts",
            robloxUserId, maxAttempts);
        throw new HttpRequestException($"Could not reach the Roblox users API for user {robloxUserId}.", lastException);
    }

    private sealed class RobloxUserDto
    {
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}