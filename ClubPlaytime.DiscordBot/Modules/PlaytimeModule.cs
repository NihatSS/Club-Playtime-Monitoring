using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Services;

namespace ClubPlaytime.DiscordBot.Modules;

public sealed class PlaytimeModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlaytimeApiClient _api;
    private readonly ILogger<PlaytimeModule> _logger;

    // Leadboard color palette
    private static readonly Color BrandPurple = new(0x9B59B6);

    private static readonly Color BrandRed = new(0xE74C3C);
    private static readonly Color BrandOrange = new(0xF39C12);
    private static readonly Color LeaderboardGold = new(0xF1C40F);

    private static readonly string[] PeriodEmotes = ["🕒", "📊", "📆", "🏆"];
    private static readonly string[] PeriodNames = ["Daily", "Weekly", "Monthly", "Total"];
    private static readonly string[] PeriodKeys = ["daily", "weekly", "monthly", "total"];

    private const int PageSize = 10;

    public PlaytimeModule(PlaytimeApiClient api, ILogger<PlaytimeModule> logger)
    {
        _api = api;
        _logger = logger;
    }

    [SlashCommand("playtime", "View playtime stats for a player")]
    public async Task PlaytimeAsync(
        [Summary(description: "Roblox username to look up (leave blank for your linked or pre-linked account)")]
        string? playerName = null)
    {
        await DeferAsync();

        PlayerDetailsDto? player = null;

        if (!string.IsNullOrWhiteSpace(playerName))
        {
            _logger.LogInformation("Looking up player by name: {Name}", playerName);
            var allPlayers = await _api.GetPlayersAsync();
            _logger.LogInformation("GetPlayersAsync returned {Count} players", allPlayers?.Count ?? 0);

            var matched = allPlayers?.FirstOrDefault(p =>
                p.Username.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (matched is not null)
            {
                _logger.LogInformation("Found player by name: {Id} {Username}", matched.Id, matched.Username);
                player = await _api.GetPlayerDetailsAsync(matched.Id);
            }
            else
            {
                _logger.LogWarning("No player matched the name '{Name}' in {Count} total players",
                    playerName, allPlayers?.Count ?? 0);
                if (allPlayers is not null && allPlayers.Count > 0)
                {
                    var names = string.Join(", ", allPlayers.Take(10).Select(p => p.Username));
                    _logger.LogWarning("First {Count} player names in DB: {Names}",
                        Math.Min(10, allPlayers.Count), names);
                }
            }
        }
        else
        {
            var discordId = Context.User.Id.ToString();
            _logger.LogInformation("Looking up player by Discord ID: {DiscordId}", discordId);
            player = await _api.GetPlayerByDiscordUserIdAsync(discordId);
        }

        if (player is null)
        {
            var eb = playerName is not null
                ? PlayerNotFoundEmbed(playerName)
                : NotLinkedEmbed();
            await FollowupAsync(embed: eb);
            return;
        }

        await SendPlaytimeEmbedAsync(player, "daily");
    }

    [SlashCommand("leaderboard", "View the playtime leaderboard")]
    public async Task LeaderboardAsync(
        [Summary(description: "Time period: daily, weekly, monthly, or total")]
        [Choice("Daily", "daily")]
        [Choice("Weekly", "weekly")]
        [Choice("Monthly", "monthly")]
        [Choice("Total (All Time)", "total")]
        string period = "daily")
    {
        await DeferAsync();

        var leaderboard = await _api.GetLeaderboardAsync(period);
        if (leaderboard is null || leaderboard.Count == 0)
        {
            var eb = new EmbedBuilder()
                .WithColor(BrandOrange)
                .WithTitle("🏆 Leaderboard")
                .WithDescription("No playtime data available for this period yet.")
                .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime • Leaderboard" })
                .WithCurrentTimestamp()
                .Build();

            await FollowupAsync(embed: eb);
            return;
        }

        var currentUserRank = await GetCurrentUserRankAsync(leaderboard);

        var (embed, totalPages) = BuildLeaderboardEmbed(leaderboard, period, page: 1, currentUserRank);
        await FollowupAsync(embed: embed, components: BuildLeaderboardComponents(period, page: 1, totalPages));
    }

    // ─── Leaderboard Interaction Handlers ─────────────────────────────────

    /// <summary>
    /// Backward-compatible handler for old leaderboard period buttons (leaderboard_daily, etc.)
    /// used by DailyPlaytimePoster. Redirects to page 1 of the selected period.
    /// </summary>
    [ComponentInteraction("leaderboard_*")]
    public async Task HandleLegacyLeaderboardButtonAsync(string period)
    {
        await DeferAsync();

        var leaderboard = await _api.GetLeaderboardAsync(period);
        if (leaderboard is null || leaderboard.Count == 0)
        {
            await FollowupAsync("No playtime data available for this period.", ephemeral: true);
            return;
        }

        var currentUserRank = await GetCurrentUserRankAsync(leaderboard);
        var totalPages = (int)Math.Ceiling((double)leaderboard.Count / PageSize);
        var (embed, _) = BuildLeaderboardEmbed(leaderboard, period, page: 1, currentUserRank);
        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = BuildLeaderboardComponents(period, page: 1, totalPages);
        });
    }

    /// <summary>Period button: lb_p_{period}_{page}</summary>
    [ComponentInteraction("lb_p_*_*")]
    public async Task HandleLeaderboardPeriodAsync(string period, string pageStr)
    {
        await DeferAsync();

        var page = int.TryParse(pageStr, out var p) ? p : 1;
        var leaderboard = await FetchAndUpdateLeaderboardAsync(period, page);
        if (leaderboard is null) return;
    }

    /// <summary>Navigation button: lb_n_{period}_{page}_{direction}</summary>
    [ComponentInteraction("lb_n_*_*_*")]
    public async Task HandleLeaderboardNavAsync(string period, string pageStr, string direction)
    {
        await DeferAsync();

        var leaderboard = await _api.GetLeaderboardAsync(period);
        if (leaderboard is null || leaderboard.Count == 0)
        {
            await FollowupAsync("No playtime data available for this period.", ephemeral: true);
            return;
        }

        var totalPages = (int)Math.Ceiling((double)leaderboard.Count / PageSize);
        var currentPage = int.TryParse(pageStr, out var p) ? p : 1;

        var newPage = direction switch
        {
            "prev" => Math.Max(1, currentPage - 1),
            "next" => Math.Min(totalPages, currentPage + 1),
            _ => currentPage
        };

        var currentUserRank = await GetCurrentUserRankAsync(leaderboard);
        var (embed, _) = BuildLeaderboardEmbed(leaderboard, period, newPage, currentUserRank);
        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = BuildLeaderboardComponents(period, newPage, totalPages);
        });
    }

    /// <summary>Go To Page button: lb_g_{period} — opens a modal</summary>
    [ComponentInteraction("lb_g_*")]
    public async Task HandleLeaderboardGoToAsync(string period)
    {
        await Context.Interaction.RespondWithModalAsync<GoToPageModal>(
            $"lb_modal_{period}",
            new GoToPageModal(),
            null);
    }

    /// <summary>Modal submission for Go To Page</summary>
    [ModalInteraction("lb_modal_*")]
    public async Task HandleLeaderboardGoToModalAsync(string period, GoToPageModal modal)
    {
        await DeferAsync();

        var leaderboard = await _api.GetLeaderboardAsync(period);
        if (leaderboard is null || leaderboard.Count == 0)
        {
            await FollowupAsync("No playtime data available for this period.", ephemeral: true);
            return;
        }

        var totalPages = (int)Math.Ceiling((double)leaderboard.Count / PageSize);
        var requestedPage = int.TryParse(modal.PageNumber, out var p) ? p : 1;
        var page = Math.Clamp(requestedPage, 1, totalPages);

        var currentUserRank = await GetCurrentUserRankAsync(leaderboard);
        var (embed, _) = BuildLeaderboardEmbed(leaderboard, period, page, currentUserRank);
        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = BuildLeaderboardComponents(period, page, totalPages);
        });
    }

    // ─── Helper Methods ──────────────────────────────────────────────────

    private async Task<List<LeaderboardPlayerDto>?> FetchAndUpdateLeaderboardAsync(string period, int page)
    {
        var leaderboard = await _api.GetLeaderboardAsync(period);
        if (leaderboard is null || leaderboard.Count == 0)
        {
            await FollowupAsync("No playtime data available for this period.", ephemeral: true);
            return null;
        }

        var totalPages = (int)Math.Ceiling((double)leaderboard.Count / PageSize);
        var clampedPage = Math.Clamp(page, 1, totalPages);
        var currentUserRank = await GetCurrentUserRankAsync(leaderboard);
        var (embed, _) = BuildLeaderboardEmbed(leaderboard, period, clampedPage, currentUserRank);
        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = BuildLeaderboardComponents(period, clampedPage, totalPages);
        });

        return leaderboard;
    }

    /// <summary>Returns the current user's rank and display name on the given leaderboard, or null if unranked.</summary>
    private async Task<(int Rank, string Username)?> GetCurrentUserRankAsync(List<LeaderboardPlayerDto> leaderboard)
    {
        var discordId = Context.User.Id.ToString();
        var currentPlayer = await _api.GetPlayerByDiscordUserIdAsync(discordId);
        if (currentPlayer is null)
            return null;

        for (var i = 0; i < leaderboard.Count; i++)
        {
            if (leaderboard[i].PlayerId == currentPlayer.Id)
                return (i + 1, currentPlayer.Username);
        }

        return null;
    }

    // ─── Embed Building ──────────────────────────────────────────────────

    private (Embed Embed, int TotalPages) BuildLeaderboardEmbed(
        List<LeaderboardPlayerDto> leaderboard,
        string period,
        int page,
        (int Rank, string Username)? currentUserRank)
    {
        var periodIndex = Array.IndexOf(PeriodKeys, period);
        var periodLabel = PeriodNames[periodIndex >= 0 ? periodIndex : 0];
        var emote = periodIndex >= 0 ? PeriodEmotes[periodIndex] : "🏆";

        var totalPages = (int)Math.Ceiling((double)leaderboard.Count / PageSize);
        var pageStart = (page - 1) * PageSize;
        var pageEntries = leaderboard.Skip(pageStart).Take(PageSize).ToList();

        var description = new System.Text.StringBuilder();
        description.AppendLine();

        for (var i = 0; i < pageEntries.Count; i++)
        {
            var entry = pageEntries[i];
            var rank = pageStart + i + 1;

            var mentionOrName = !string.IsNullOrWhiteSpace(entry.DiscordUserId)
                ? $"<@{entry.DiscordUserId}>"
                : $"@**{entry.Username}**";
            var periodFormatted = FormatDuration(entry.PlaySeconds);

            description.AppendLine($"  **{rank}.** {mentionOrName} - {periodFormatted}");
        }

        // Current user's position, styled like a small badge line beneath the list
        if (currentUserRank.HasValue)
        {
            description.AppendLine();
            description.AppendLine($"**Your Position**: #{currentUserRank.Value.Rank}");
        }

        var embed = new EmbedBuilder()
            .WithColor(LeaderboardGold)
            .WithTitle($"🏆 Leaderboard {periodLabel} (Page {page}/{totalPages})")
            .WithDescription(description.ToString())
            .WithFooter(new EmbedFooterBuilder
            {
                Text = $"{emote} {periodLabel} • {leaderboard.Count} player{(leaderboard.Count != 1 ? "s" : "")} tracked"
            })
            .WithCurrentTimestamp();

        // Add top player's avatar as thumbnail
        if (leaderboard.Count > 0 && leaderboard[0].AvatarUrl is not null)
        {
            embed.WithThumbnailUrl(leaderboard[0].AvatarUrl);
        }

        return (embed.Build(), totalPages);
    }

    // ─── Component Builders ──────────────────────────────────────────────

    private static MessageComponent BuildLeaderboardComponents(string period, int page, int totalPages)
    {
        var builder = new ComponentBuilder();

        // Row 1: Page navigation
        var isFirstPage = page <= 1;
        var isLastPage = page >= totalPages;

        builder.WithButton("◀", $"lb_n_{period}_{page}_prev", ButtonStyle.Secondary, disabled: isFirstPage);
        builder.WithButton("Go To Page", $"lb_g_{period}", ButtonStyle.Secondary);
        builder.WithButton("▶", $"lb_n_{period}_{page}_next", ButtonStyle.Secondary, disabled: isLastPage);

        // Row 2: Period selection
        for (var i = 0; i < PeriodKeys.Length; i++)
        {
            var isActive = PeriodKeys[i] == period;
            builder.WithButton(
                label: PeriodNames[i],
                customId: $"lb_p_{PeriodKeys[i]}_{page}",
                style: isActive ? ButtonStyle.Primary : ButtonStyle.Secondary,
                emote: new Emoji(PeriodEmotes[i]),
                disabled: isActive,
                row: 1);
        }

        return builder.Build();
    }

    // ─── Existing private helpers ────────────────────────────────────────

    private async Task SendPlaytimeEmbedAsync(PlayerDetailsDto player, string period)
    {
        var embed = BuildPlaytimeEmbed(player, period);
        await FollowupAsync(embed: embed);
    }

    private Embed BuildPlaytimeEmbed(PlayerDetailsDto player, string period)
    {
        var dailyFormatted = FormatDuration(player.TodayPlaySeconds);
        var weeklyFormatted = FormatDuration(player.WeeklyPlaySeconds);
        var monthlyFormatted = FormatDuration(player.MonthlyPlaySeconds);
        var totalFormatted = FormatDuration(player.TotalPlaySeconds);

        // Determine status display
        var (statusEmoji, statusText) = player.CurrentStatus switch
        {
            null or "" => ("⚪", "Offline"),
            "Online" => ("🟢", "Online"),
            string s when s.Contains("Playing", StringComparison.OrdinalIgnoreCase) => ("🎮", s),
            _ => ("🟢", player.CurrentStatus)
        };

        // Club display
        var clubDisplay = string.IsNullOrWhiteSpace(player.Club) ? "No Club" : player.Club;

        var description = new System.Text.StringBuilder();
        description.AppendLine("**Playtime:**");
        description.AppendLine();
        description.AppendLine($"**Today:** {dailyFormatted}");
        description.AppendLine($"**This Week:** {weeklyFormatted}");
        description.AppendLine($"**This Month:** {monthlyFormatted}");
        description.AppendLine($"**Total:** {totalFormatted}");
        description.AppendLine();
        description.AppendLine($"**Status:** {statusEmoji} {statusText}");
        description.AppendLine($"**Club:** {clubDisplay}");

        var eb = new EmbedBuilder()
            .WithColor(BrandPurple)
            .WithTitle($"{player.Username}")
            .WithUrl(player.ProfileUrl)
            .WithDescription(description.ToString())
            .WithThumbnailUrl(player.AvatarUrl)
            .WithFooter(new EmbedFooterBuilder
            {
                Text = "Club Playtime Tracker"
            })
            .WithCurrentTimestamp()
            .Build();

        return eb;
    }

    private Embed PlayerNotFoundEmbed(string playerName)
    {
        return new EmbedBuilder()
            .WithColor(BrandRed)
            .WithAuthor(new EmbedAuthorBuilder
            {
                Name = "❌ Player Not Found",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            })
            .WithDescription($"Could not find **{playerName}** in the database.")
            .AddField("Did you mean to?", "Use `/link <username>` to link your account, or check the spelling and try again.")
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
                Name = "⚠️ No Linked Account",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            })
            .WithDescription("You don't have a linked account yet.")
            .AddField("Already in the tracker?",
                      "Ask an admin to set your Discord ID, then you're good to go!")
            .AddField("Not added yet?",
                      "Use `/link <username>` to link your Roblox account.")
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime" })
            .WithCurrentTimestamp()
            .Build();
    }

    internal static string FormatDuration(long totalSeconds)
    {
        var absSeconds = Math.Abs(totalSeconds);
        var hours = absSeconds / 3600;
        var minutes = (absSeconds % 3600) / 60;
        var seconds = absSeconds % 60;

        if (hours > 0)
            return $"{hours}h {minutes:00}m {seconds:00}s";
        if (minutes > 0)
            return $"{minutes}m {seconds:00}s";
        return $"{seconds}s";
    }


}

// ─── Modal for Go To Page ────────────────────────────────────────────────

public sealed class GoToPageModal : IModal
{
    public string Title => "Go to Page";

    [InputLabel("Page Number")]
    [ModalTextInput("page_number", TextInputStyle.Short, placeholder: "Enter a page number...", minLength: 1, maxLength: 4)]
    public string PageNumber { get; set; } = string.Empty;
}