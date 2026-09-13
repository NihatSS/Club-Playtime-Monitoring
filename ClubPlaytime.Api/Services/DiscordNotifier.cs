using System.Net.Http.Json;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed class DiscordNotifier(
    HttpClient client,
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

    /// <summary>Announces newly unlocked achievements as a rich embed with the player's avatar.</summary>
    public Task AchievementUnlockedAsync(
        Player player,
        IReadOnlyList<(string Key, string Name, string Icon, DateTime? CrossedAt)> achievements,
        CancellationToken cancellationToken = default)
    {
        if (achievements.Count == 0)
        {
            return Task.CompletedTask;
        }

        var title = achievements.Count == 1
            ? $"{achievements[0].Icon} {player.Username} unlocked an achievement!"
            : $"🏆 {player.Username} unlocked {achievements.Count} achievements!";

        var description = string.Join(
            "\n",
            achievements.Select(a =>
                a.CrossedAt is { } crossed
                    ? $"{a.Icon} **{a.Name}** — reached on {crossed:MMM d, yyyy}"
            : $"{a.Icon} **{a.Name}**"));

        return SendEmbedAsync(new
        {
            embeds = new[]
            {
                new
                {
                    title,
                    description,
                    color = 0xFFAB00, // neon amber, matches the site's achievement accent
                    thumbnail = new { url = player.AvatarUrl ?? string.Empty },
                    footer = new { text = "Club Playtime Tracker — Achievements" },
                    timestamp = DateTime.UtcNow
                }
            }
        }, cancellationToken);
    }

    /// <summary>Announces a playtime streak milestone as a rich embed with the player's avatar.</summary>
    public Task StreakMilestoneAsync(Player player, int streakDays, CancellationToken cancellationToken = default)
    {
        var fireEmoji = streakDays >= 30 ? "☄️" : "🔥";
        return SendEmbedAsync(new
        {
            embeds = new[]
            {
                new
                {
                    title = $"{fireEmoji} {player.Username} hit a {streakDays}-day streak!",
                    description = $"{streakDays} consecutive days of recorded playtime.",
                    color = 0x39FF14, // neon green, matches the site's streak accent
                    thumbnail = new { url = player.AvatarUrl ?? string.Empty },
                    footer = new { text = "Club Playtime Tracker — Streaks" },
                    timestamp = DateTime.UtcNow
                }
            }
        }, cancellationToken);
    }

    private async Task SendEmbedAsync(object payload, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (!settings.EnableDiscordNotifications || string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            return;
        }

        try
        {
            using var response = await client.PostAsJsonAsync(settings.DiscordWebhookUrl, payload, cancellationToken);
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

    private async Task SendAsync(string content, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (!settings.EnableDiscordNotifications || string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            return;
        }

        try
        {
            using var response = await client.PostAsJsonAsync(settings.DiscordWebhookUrl, new { content }, cancellationToken);
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
