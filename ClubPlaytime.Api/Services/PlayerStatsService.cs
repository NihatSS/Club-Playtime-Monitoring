using System.Text;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Repositories;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed class PlayerStatsService(
    IPlayerRepository playerRepository,
    IDailyPlaytimeRepository dailyPlaytimeRepository,
    IActivityRepository activityRepository,
    IRobloxAvatarClient avatarClient,
    IOptionsMonitor<MonitoringOptions> options) : IPlayerStatsService
{
    public async Task<IReadOnlyList<PlayerDto>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        var players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
        var todayTotals = await dailyPlaytimeRepository.GetPlaySecondsForDateAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

        return players.Select(player => ToPlayerDto(player, todayTotals.GetValueOrDefault(player.Id))).ToList();
    }

    public async Task<PlayerDetailsDto?> GetPlayerDetailsAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.GetByIdAsync(playerId, trackChanges: false, cancellationToken);
        if (player is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var last30From = today.AddDays(-29);
        var weekFrom = today.AddDays(-6);
        var dailyRows = await dailyPlaytimeRepository.GetRangeAsync(player.Id, last30From, today, cancellationToken);
        var recentActivity = await activityRepository.GetRecentForPlayerAsync(player.Id, 20, cancellationToken);
        var dailyByDate = dailyRows.ToDictionary(row => row.Date, row => row.PlaySeconds);
        var last30Days = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var date = last30From.AddDays(offset);
                return new DailyPlaytimeDto(date, dailyByDate.GetValueOrDefault(date));
            })
            .ToList();

        var todaySeconds = dailyByDate.GetValueOrDefault(today);
        var weeklySeconds = dailyRows.Where(row => row.Date >= weekFrom).Sum(row => row.PlaySeconds);
        var monthlySeconds = dailyRows.Sum(row => row.PlaySeconds);

        return new PlayerDetailsDto(
            player.Id,
            player.Username,
            player.ProfileUrl,
            player.RobloxUserId,
            GetStatus(player),
            player.CurrentlyPlaying,
            player.LastSeenPlaying,
            todaySeconds,
            weeklySeconds,
            monthlySeconds,
            player.TotalPlaySeconds,
            player.AvatarUrl,
            player.Club,
            player.DiscordUserId,
            last30Days,
            recentActivity.Select(ToActivityDto).ToList());
    }

    public async Task<PlayerDto> AddPlayerAsync(AddPlayerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await playerRepository.GetByRobloxUserIdAsync(request.RobloxUserId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A player with this Roblox user ID already exists.");
        }

        var now = DateTime.UtcNow;
        var baseUrl = options.CurrentValue.RobloxBaseUrl.TrimEnd('/');
        var player = new Player
        {
            Username = request.Username.Trim(),
            RobloxUserId = request.RobloxUserId,
            Club = request.Club ?? "PIH",
            DiscordUserId = string.IsNullOrWhiteSpace(request.DiscordUserId) ? null : request.DiscordUserId.Trim(),
            ProfileUrl = $"{baseUrl}/users/{request.RobloxUserId}/profile",
            CreatedAt = now,
            UpdatedAt = now,
            AvatarUrl = await avatarClient.GetAvatarUrlAsync(request.RobloxUserId, cancellationToken)
        };

        await playerRepository.AddAsync(player, cancellationToken);
        await playerRepository.SaveChangesAsync(cancellationToken);
        return ToPlayerDto(player, 0);
    }

    public async Task<bool> DeletePlayerAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.GetByIdAsync(playerId, cancellationToken: cancellationToken);
        if (player is null)
        {
            return false;
        }

        playerRepository.Remove(player);
        await playerRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PlayerDetailsDto?> AdjustPlaytimeAsync(int playerId, AdjustPlaytimeRequest request, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.GetByIdAsync(playerId, cancellationToken: cancellationToken);
        if (player is null)
        {
            return null;
        }

        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dailyPlaytime = await dailyPlaytimeRepository.GetOrCreateAsync(player.Id, date, cancellationToken);
        var actualDelta = request.DeltaSeconds;
        if (actualDelta < 0)
        {
            actualDelta = Math.Max(actualDelta, -Math.Min(dailyPlaytime.PlaySeconds, player.TotalPlaySeconds));
        }

        dailyPlaytime.PlaySeconds += actualDelta;
        player.TotalPlaySeconds += actualDelta;
        player.UpdatedAt = DateTime.UtcNow;

        await activityRepository.AddAsync(new PlayerActivityEvent
        {
            PlayerId = player.Id,
            EventType = "Adjusted",
            Message = string.IsNullOrWhiteSpace(request.Reason)
                ? $"Manual playtime adjustment: {actualDelta} seconds."
                : $"Manual playtime adjustment: {actualDelta} seconds. {request.Reason.Trim()}",
            DeltaSeconds = actualDelta,
            OccurredAt = player.UpdatedAt
        }, cancellationToken);

        await playerRepository.SaveChangesAsync(cancellationToken);
        return await GetPlayerDetailsAsync(playerId, cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardPlayerDto>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
        var todayTotals = await dailyPlaytimeRepository.GetPlaySecondsForDateAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

        return players.Select(player => new DashboardPlayerDto(
            player.Id,
            player.Username,
            player.ProfileUrl,
            player.RobloxUserId,
            GetStatus(player),
            player.CurrentlyPlaying,
            player.LastSeenPlaying,
            todayTotals.GetValueOrDefault(player.Id),
            player.TotalPlaySeconds,
            player.AvatarUrl,
            player.Club,
            player.DiscordUserId)).ToList();
    }

    public async Task<IReadOnlyList<LeaderboardPlayerDto>> GetLeaderboardAsync(string period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = period.ToLowerInvariant();
        var validPeriods = new[] { "daily", "weekly", "monthly", "total" };
        if (!validPeriods.Contains(normalizedPeriod))
        {
            throw new ArgumentException($"Invalid period '{period}'. Valid values: {string.Join(", ", validPeriods)}", nameof(period));
        }

        var players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var fromDate = normalizedPeriod switch
        {
            "daily" => today,
            "weekly" => today.AddDays(-6),
            "monthly" => today.AddDays(-29),
            _ => DateOnly.MinValue // total - get all
        };

        Dictionary<int, long> periodTotals;
        if (fromDate == DateOnly.MinValue)
        {
            // Total: use player.TotalPlaySeconds
            return players
                .Select(player => new LeaderboardPlayerDto(
                    player.Id,
                    player.Username,
                    player.AvatarUrl,
                    player.TotalPlaySeconds,
                    player.TotalPlaySeconds,
                    player.DiscordUserId))
                .OrderByDescending(p => p.PlaySeconds)
                .ThenBy(p => p.Username)
                .ToList();
        }

        periodTotals = await dailyPlaytimeRepository.GetPlaySecondsSinceAsync(fromDate, cancellationToken);

        return players
            .Select(player => new LeaderboardPlayerDto(
                player.Id,
                player.Username,
                player.AvatarUrl,
                periodTotals.GetValueOrDefault(player.Id),
                player.TotalPlaySeconds,
                player.DiscordUserId))
            .OrderByDescending(player => player.PlaySeconds)
            .ThenBy(player => player.Username)
            .ToList();
    }

    public async Task<PlayerDetailsDto?> GetPlayerByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.GetByDiscordUserIdAsync(discordUserId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        return await GetPlayerDetailsAsync(player.Id, cancellationToken);
    }

    public async Task<PlayerDetailsDto?> LinkDiscordUserAsync(string robloxUsername, string discordUserId, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.GetByUsernameAsync(robloxUsername, cancellationToken);
        if (player is null)
        {
            return null;
        }

        // If this Discord ID is already linked to the same player, just return ok
        if (string.Equals(player.DiscordUserId, discordUserId, StringComparison.Ordinal))
        {
            return await GetPlayerDetailsAsync(player.Id, cancellationToken);
        }

        // If this Discord ID is already linked to a different player, unlink that one first
        var existingPlayerWithSameDiscord = await playerRepository.GetByDiscordUserIdAsync(discordUserId, cancellationToken);
        if (existingPlayerWithSameDiscord is not null && existingPlayerWithSameDiscord.Id != player.Id)
        {
            existingPlayerWithSameDiscord.DiscordUserId = null;
            existingPlayerWithSameDiscord.UpdatedAt = DateTime.UtcNow;
        }

        player.DiscordUserId = discordUserId;
        player.UpdatedAt = DateTime.UtcNow;
        await playerRepository.SaveChangesAsync(cancellationToken);
        return await GetPlayerDetailsAsync(player.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklyLeaderboardDto>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
        var weekFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6);
        var weeklyTotals = await dailyPlaytimeRepository.GetPlaySecondsSinceAsync(weekFrom, cancellationToken);

        return players
            .Select(player => new WeeklyLeaderboardDto(
                player.Id,
                player.Username,
                player.AvatarUrl,
                weeklyTotals.GetValueOrDefault(player.Id),
                player.TotalPlaySeconds))
            .OrderByDescending(player => player.WeeklyPlaySeconds)
            .ThenBy(player => player.Username)
            .ToList();
    }

    public async Task<string> ExportCsvAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
        var startDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-29);
        var endDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var csv = new StringBuilder();
        csv.AppendLine("Username,RobloxUserId,Date,PlaySeconds,Playtime");

        foreach (var player in players)
        {
            var rows = await dailyPlaytimeRepository.GetRangeAsync(player.Id, startDate, endDate, cancellationToken);
            foreach (var row in rows)
            {
                csv.Append(Csv(player.Username)).Append(',')
                    .Append(player.RobloxUserId).Append(',')
                    .Append(row.Date.ToString("yyyy-MM-dd")).Append(',')
                    .Append(row.PlaySeconds).Append(',')
                    .Append(Csv(FormatDuration(row.PlaySeconds)))
                    .AppendLine();
            }
        }

        return csv.ToString();
    }

    private PlayerDto ToPlayerDto(Player player, long todayPlaySeconds)
    {
        return new PlayerDto(
            player.Id,
            player.Username,
            player.ProfileUrl,
            player.RobloxUserId,
            GetStatus(player),
            player.IsOnline,
            player.CurrentlyPlaying,
            player.LastSeenPlaying,
            todayPlaySeconds,
            player.TotalPlaySeconds,
            player.AvatarUrl,
            player.Club,
            player.DiscordUserId,
            player.CreatedAt,
            player.UpdatedAt);
    }

    private ActivityEventDto ToActivityDto(PlayerActivityEvent activityEvent)
    {
        return new ActivityEventDto(
            activityEvent.Id,
            activityEvent.EventType,
            activityEvent.Message,
            activityEvent.DeltaSeconds,
            activityEvent.OccurredAt);
    }

    private string GetStatus(Player player)
    {
        if (player.LastSeenPlaying.HasValue &&
            player.CurrentlyPlaying?.Contains(options.CurrentValue.TargetGameName, StringComparison.OrdinalIgnoreCase) == true)
        {
            return $"Playing {options.CurrentValue.TargetGameName}";
        }

        return player.IsOnline ? "Online" : "Offline";
    }

    private static string Csv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string FormatDuration(long totalSeconds)
    {
        var totalMinutes = Math.Max(0, totalSeconds) / 60;
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return hours > 0 ? $"{hours}h {minutes:00}m" : $"{minutes}m";
    }
}
