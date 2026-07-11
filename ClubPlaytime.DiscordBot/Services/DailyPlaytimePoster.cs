using Discord;
using Discord.WebSocket;
using ClubPlaytime.DiscordBot.Modules;

namespace ClubPlaytime.DiscordBot.Services;

public sealed class DailyPlaytimePoster : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyPlaytimePoster> _logger;
    private readonly IConfiguration _configuration;
    private TimeOnly _postTime;

    public DailyPlaytimePoster(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyPlaytimePoster> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;

        var timeStr = configuration["Discord:DailyPostTime"] ?? "08:00";
        _postTime = TimeOnly.TryParse(timeStr, out var parsed) ? parsed : new TimeOnly(8, 0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily playtime poster started. Scheduled for {PostTime:HH:mm} UTC.", _postTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = TimeOnly.FromDateTime(DateTime.UtcNow);
                var nextRun = now <= _postTime
                    ? DateTime.UtcNow.Date.Add(_postTime.ToTimeSpan())
                    : DateTime.UtcNow.Date.AddDays(1).Add(_postTime.ToTimeSpan());

                var delay = nextRun - DateTime.UtcNow;
                _logger.LogDebug("Next daily post at {NextRun:yyyy-MM-dd HH:mm:ss} UTC (in {DelayMinutes:F0} minutes)",
                    nextRun, delay.TotalMinutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                await PostDailySummaryAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting daily playtime summary.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task PostDailySummaryAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
        var api = scope.ServiceProvider.GetRequiredService<PlaytimeApiClient>();

        var channelIdStr = _configuration["Discord:DailyChannelId"];
        if (string.IsNullOrWhiteSpace(channelIdStr))
        {
            _logger.LogWarning("DailyChannelId not set. Skipping daily post.");
            return;
        }

        if (!ulong.TryParse(channelIdStr, out var channelId))
        {
            _logger.LogWarning("Invalid DailyChannelId: {ChannelId}", channelIdStr);
            return;
        }

        var channel = client.GetChannel(channelId) as IMessageChannel;
        if (channel is null)
        {
            _logger.LogWarning("Could not find channel with ID {ChannelId}", channelId);
            return;
        }

        // Fetch leaderboard data
        var dailyLeaderboard = await api.GetLeaderboardAsync("daily", cancellationToken);
        var weeklyLeaderboard = await api.GetLeaderboardAsync("weekly", cancellationToken);

        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("MMMM dd, yyyy");
        var description = new System.Text.StringBuilder();

        // Daily Top 10 section
        if (dailyLeaderboard is not null && dailyLeaderboard.Count > 0)
        {
            description.AppendLine("📅 **Daily Top 10**");
            var topDaily = dailyLeaderboard.Take(10).ToList();
            foreach (var (entry, i) in topDaily.Select((e, i) => (e, i)))
            {
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"#{i + 1}" };
                description.AppendLine($"{medal} **{entry.Username}** — {PlaytimeModule.FormatDuration(entry.PlaySeconds)}");
            }
            description.AppendLine();
        }

        // Weekly Top 10 section
        if (weeklyLeaderboard is not null && weeklyLeaderboard.Count > 0)
        {
            description.AppendLine("📊 **Weekly Top 10**");
            var topWeekly = weeklyLeaderboard.Take(10).ToList();
            foreach (var (entry, i) in topWeekly.Select((e, i) => (e, i)))
            {
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"#{i + 1}" };
                description.AppendLine($"{medal} **{entry.Username}** — {PlaytimeModule.FormatDuration(entry.PlaySeconds)}");
            }
        }

        // Fallback if no data
        if (description.Length == 0)
        {
            description.AppendLine("No playtime data available for yesterday.");
        }

        var goldColor = new Color(0xF1C40F);
        var embed = new EmbedBuilder()
            .WithColor(goldColor)
            .WithTitle($"🌅 Daily Summary — {yesterday}")
            .WithDescription(description.ToString())
            .WithFooter(new EmbedFooterBuilder { Text = "Club Playtime Tracker • Daily Report" })
            .WithCurrentTimestamp();

        // Add top player's avatar as thumbnail if available
        var topPlayer = dailyLeaderboard?.FirstOrDefault() ?? weeklyLeaderboard?.FirstOrDefault();
        if (topPlayer?.AvatarUrl is not null)
        {
            embed.WithThumbnailUrl(topPlayer.AvatarUrl);
        }

        var components = new ComponentBuilder()
            .WithButton("📅 Daily", "leaderboard_daily", ButtonStyle.Secondary)
            .WithButton("📊 Weekly", "leaderboard_weekly", ButtonStyle.Secondary)
            .WithButton("📆 Monthly", "leaderboard_monthly", ButtonStyle.Secondary)
            .WithButton("🏆 Total", "leaderboard_total", ButtonStyle.Secondary);

        await channel.SendMessageAsync(embed: embed.Build(), components: components.Build());
        _logger.LogInformation("Daily playtime summary posted to channel {ChannelId}.", channelId);
    }
}
