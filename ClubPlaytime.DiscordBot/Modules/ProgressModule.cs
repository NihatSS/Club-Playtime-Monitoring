using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Services;

namespace ClubPlaytime.DiscordBot.Modules;

/// <summary>
/// Slash commands for the achievements and streak features. Uses the exact
/// same data as the website (stats/players endpoints), the same
/// Discord → website account → tracker player linking as /playtime, and the
/// same not-linked / not-found / API-unavailable responses.
/// </summary>
public sealed class ProgressModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlaytimeApiClient _api;
    private readonly ILogger<ProgressModule> _logger;
    private readonly IConfiguration _configuration;

    private static readonly Color BrandPurple = new(0xB347EA); // site's neon purple
    private static readonly Color BrandGreen = new(0x39FF14);  // site's streak green
    private static readonly Color BrandRed = new(0xE74C3C);
    private static readonly Color BrandOrange = new(0xF39C12);

    public ProgressModule(PlaytimeApiClient api, ILogger<ProgressModule> logger, IConfiguration configuration)
    {
        _api = api;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>Origin of the tracker website, derived from the configured API base URL.</summary>
    private string SiteUrl
    {
        get
        {
            var apiBaseUrl = _configuration["Api:BaseUrl"];
            if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
            {
                return uri.GetLeftPart(UriPartial.Authority);
            }

            return "https://rrplaytimetracker.online";
        }
    }

    [SlashCommand("achievements", "View achievements and badges for a player")]
    public async Task AchievementsAsync(
        [Summary(description: "Roblox username to look up (leave blank to check your own account)")]
        string? playerName = null)
    {
        await DeferAsync();

        var player = await ResolvePlayerOrNullAsync(playerName);
        if (player is null) return; // an error embed has already been sent

        PlayerAchievementsDto? achievements;
        try
        {
            achievements = await _api.GetPlayerAchievementsAsync(player.Id);
        }
        catch (ApiUnavailableException ex)
        {
            _logger.LogError(ex, "Playtime API unreachable while handling /achievements");
            await FollowupAsync(embed: ApiUnavailableEmbed());
            return;
        }

        if (achievements is null)
        {
            await FollowupAsync(embed: PlayerNotFoundEmbed(playerName ?? player.Username));
            return;
        }

        await FollowupAsync(embed: BuildAchievementsEmbed(achievements));
    }

    [SlashCommand("streak", "View the playtime streak for a player")]
    public async Task StreakAsync(
        [Summary(description: "Roblox username to look up (leave blank to check your own account)")]
        string? playerName = null)
    {
        await DeferAsync();

        var player = await ResolvePlayerOrNullAsync(playerName);
        if (player is null) return; // an error embed has already been sent

        PlayerStatsDto? stats;
        try
        {
            stats = await _api.GetPlayerStatsAsync(player.Id);
        }
        catch (ApiUnavailableException ex)
        {
            _logger.LogError(ex, "Playtime API unreachable while handling /streak");
            await FollowupAsync(embed: ApiUnavailableEmbed());
            return;
        }

        if (stats is null)
        {
            await FollowupAsync(embed: PlayerNotFoundEmbed(playerName ?? player.Username));
            return;
        }

        await FollowupAsync(embed: BuildStreakEmbed(stats));
    }

    // ─── Player resolution (same flow as /playtime) ──────────────────────

    /// <summary>
    /// Resolves the player to show: explicit username lookup with Discord-ID
    /// fallback, else the calling user's linked tracker player. Mirrors
    /// /playtime exactly — including which error embed is sent — and returns
    /// null when an error response has already been sent.
    /// </summary>
    private async Task<PlayerDetailsDto?> ResolvePlayerOrNullAsync(string? playerName)
    {
        PlayerDetailsDto? player;
        try
        {
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                _logger.LogInformation("Looking up player by name: {Name}", playerName);
                var allPlayers = await _api.GetPlayersAsync();

                var matched = allPlayers?.FirstOrDefault(p =>
                    p.Username.Equals(playerName, StringComparison.OrdinalIgnoreCase));

                if (matched is not null)
                {
                    _logger.LogInformation("Found player by name: {Id} {Username}", matched.Id, matched.Username);
                    // The stats endpoints return username/avatar themselves; only
                    // the id is needed. Keep the details call so a deleted player
                    // is reported the same way as /playtime.
                    player = await _api.GetPlayerDetailsAsync(matched.Id) ?? new PlayerDetailsDto
                    {
                        Id = matched.Id,
                        Username = matched.Username,
                        AvatarUrl = matched.AvatarUrl,
                        ProfileUrl = matched.ProfileUrl
                    };
                    return player;
                }

                _logger.LogWarning("No player matched the name '{Name}'; falling back to Discord ID lookup", playerName);
                var discordId = Context.User.Id.ToString();
                player = await _api.GetPlayerByDiscordUserIdAsync(discordId);
                if (player is null)
                {
                    // Same as /playtime: a named lookup that matched nothing and
                    // whose Discord fallback also failed is "player not found".
                    await FollowupAsync(embed: PlayerNotFoundEmbed(playerName));
                }
                return player;
            }

            var callerDiscordId = Context.User.Id.ToString();
            _logger.LogInformation("Looking up player by Discord ID: {DiscordId}", callerDiscordId);
            player = await _api.GetPlayerByDiscordUserIdAsync(callerDiscordId);
            if (player is null)
            {
                // Same as /playtime with no argument: the Discord account has no
                // linked tracker profile.
                await FollowupAsync(embed: NotLinkedEmbed());
            }
            return player;
        }
        catch (ApiUnavailableException ex)
        {
            _logger.LogError(ex, "Playtime API unreachable while resolving the player");
            await FollowupAsync(embed: ApiUnavailableEmbed());
            return null;
        }
    }

    // ─── Embed builders ──────────────────────────────────────────────────

    private Embed BuildAchievementsEmbed(PlayerAchievementsDto achievements)
    {
        var description = new System.Text.StringBuilder();
        description.AppendLine(
            $"**{achievements.UnlockedCount}** of **{achievements.TotalCount}** achievements unlocked");
        description.AppendLine();

        foreach (var achievement in achievements.Achievements)
        {
            if (achievement.Unlocked)
            {
                description.AppendLine($"✅ {achievement.Icon} **{achievement.Name}**");
                description.AppendLine($"╰ {achievement.Description}");
                if (achievement.UnlockedAt is { } unlockedAt)
                {
                    description.AppendLine($"╰ Unlocked {FormatDay(unlockedAt)}");
                }
            }
            else
            {
                description.AppendLine($"🔒 {achievement.Icon} **{achievement.Name}**");
                description.AppendLine($"╰ {achievement.Description}");
                if (achievement.ProgressPercent > 0 && !string.IsNullOrWhiteSpace(achievement.ProgressLabel))
                {
                    description.AppendLine($"╰ {achievement.ProgressLabel} • {achievement.ProgressPercent}% {ProgressBar(achievement.ProgressPercent)}");
                }
            }
        }

        var embed = new EmbedBuilder()
            .WithColor(achievements.UnlockedCount == achievements.TotalCount ? BrandGreen : BrandPurple)
            .WithTitle($"🏅 {achievements.Username} — Achievements ({achievements.UnlockedCount}/{achievements.TotalCount})")
            .WithUrl($"{SiteUrl}/#/achievements/{achievements.PlayerId}")
            .WithDescription(description.ToString())
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime • Achievements" })
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(achievements.AvatarUrl))
        {
            embed.WithThumbnailUrl(achievements.AvatarUrl);
        }

        return embed.Build();
    }

    private Embed BuildStreakEmbed(PlayerStatsDto stats)
    {
        var lastActive = stats.LastActiveDate is { } day
            && DateTime.TryParseExact(day, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var parsed)
                ? parsed.ToString("MMM d, yyyy")
                : null;

        var description = new System.Text.StringBuilder();
        description.AppendLine("**Streak:**");
        description.AppendLine();
        description.AppendLine($"🔥 **Current:** {PluralDays(stats.CurrentStreak)}");
        description.AppendLine($"🏆 **Longest:** {PluralDays(stats.LongestStreak)}");
        description.AppendLine($"📅 **Days played:** {stats.DaysPlayed}");
        description.AppendLine($"⏰ **Last active:** {lastActive ?? "Never"}");

        if (stats.CurrentStreak == 0 && stats.DaysPlayed > 0)
        {
            description.AppendLine();
            description.AppendLine("*Streak lapsed — record playtime today to start a new one.*");
        }

        var embed = new EmbedBuilder()
            .WithColor(stats.CurrentStreak > 0 ? BrandGreen : BrandPurple)
            .WithTitle($"🔥 {stats.Username} — Streak")
            .WithUrl($"{SiteUrl}/#/stats/{stats.PlayerId}")
            .WithDescription(description.ToString())
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime • Streaks" })
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(stats.AvatarUrl))
        {
            embed.WithThumbnailUrl(stats.AvatarUrl);
        }

        return embed.Build();
    }

    // ─── Small helpers ───────────────────────────────────────────────────

    private static string PluralDays(int days) => $"{days} day{(days == 1 ? "" : "s")}";

    /// <summary>A 10-segment progress bar, e.g. ▰▰▰▱▱▱▱▱▱▱.</summary>
    private static string ProgressBar(int percent)
    {
        var filled = Math.Clamp(percent / 10, 0, 10);
        return new string('▰', filled) + new string('▱', 10 - filled);
    }

    private static string FormatDay(DateTime utc)
        => utc.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

    // ─── Error embeds (same content/behavior as /playtime) ───────────────

    private Embed PlayerNotFoundEmbed(string playerName)
    {
        return new EmbedBuilder()
            .WithColor(BrandRed)
            .WithAuthor(new EmbedAuthorBuilder
            {
                Name = "❌ Player Not Found",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            })
            .WithDescription($"Could not find **{playerName}** in the tracker database.")
            .AddField("Not in the tracker?",
                      $"Create an account at **{SiteUrl}**, add your Roblox username and Discord ID, then **Request to Join**. An admin will review your request.")
            .AddField("Already in the tracker?",
                      "Ask an admin to link your Discord ID to your tracker profile (they can do it on the website).")
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime" })
            .WithCurrentTimestamp()
            .Build();
    }

    private Embed NotLinkedEmbed()
    {
        return new EmbedBuilder()
            .WithColor(BrandOrange)
            .WithAuthor(new EmbedAuthorBuilder
            {
                Name = "⚠️ Not Linked",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            })
            .WithDescription("I couldn't find a tracker profile linked to your Discord account.")
            .AddField("Already in the tracker?",
                      $"Your tracker profile exists but isn't linked to this Discord account yet. Ask an admin to link your Discord ID on the website, or claim your existing profile from the **{SiteUrl}** account page.")
            .AddField("Not in the tracker yet?",
                      $"Create an account at **{SiteUrl}**, add your Roblox username and Discord ID, then **Request to Join**. An admin will review your request and add you.")
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime" })
            .WithCurrentTimestamp()
            .Build();
    }

    private Embed ApiUnavailableEmbed()
    {
        return new EmbedBuilder()
            .WithColor(BrandRed)
            .WithAuthor(new EmbedAuthorBuilder
            {
                Name = "⚠️ Tracker Unavailable",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            })
            .WithDescription("The playtime tracker API couldn't be reached right now. Please try again in a few minutes.")
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime" })
            .WithCurrentTimestamp()
            .Build();
    }
}
