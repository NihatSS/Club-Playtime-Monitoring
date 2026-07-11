using System.Net.Http.Json;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed class DiscordNotifier(
    HttpClient httpClient,
    IOptionsMonitor<MonitoringOptions> options,
    ILogger<DiscordNotifier> logger) : IDiscordNotifier
{
    public Task PlayerStartedAsync(Player player, string gameName, CancellationToken cancellationToken = default)
    {
        return SendAsync($"{player.Username} started playing {gameName}.", cancellationToken);
    }

    public Task PlayerStoppedAsync(Player player, string gameName, CancellationToken cancellationToken = default)
    {
        return SendAsync($"{player.Username} stopped playing {gameName}.", cancellationToken);
    }

    private async Task SendAsync(string content, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (!settings.EnableDiscordNotifications || string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            return;
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(settings.DiscordWebhookUrl, new { content }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Discord webhook returned {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send Discord webhook notification.");
        }
    }
}
