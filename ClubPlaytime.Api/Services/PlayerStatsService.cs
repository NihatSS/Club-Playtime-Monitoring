using System.Diagnostics;
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
    IRobloxGameInfoClient gameInfoClient,
    IOptionsMonitor<MonitoringOptions> options,
    PlayerProgressService progressService,
    IPlayerMonitorRunner monitorRunner) : IPlayerStatsService
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

        // Note: these two queries must stay sequential — they share one DbContext
        // and EF Core does not allow concurrent operations on a single context.
        // The client renders the panel instantly from the dashboard list data and
        // only merges this response (chart + monthly stats) when it arrives.
        var dailyRows = await dailyPlaytimeRepository.GetRangeAsync(player.Id, last30From, today, cancellationToken);
        var recentActivity = await activityRepository.GetRecentForPlayerAsync(player.Id, 20, cancellationToken);

        // Recent Activity as game sessions: pair Started/Stopped events into
        // sessions (target-game events are reliably paired by the monitor; other
        // games get a synthetic end at the next check) and attach the real game
        // name and icon. Additive: a failure in game-info lookup must never break
        // the details response.
        List<GameSessionDto>? gameSessions = null;
        try
        {
            gameSessions = await BuildGameSessionsAsync(recentActivity, cancellationToken);
        }
        catch (Exception gameInfoEx)
        {
            // Ignored: RecentActivity below still renders the plain event feed.
        }
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

        // Compact progress summary (streaks, achievements, rank). Kept separate
        // from the main query chain so it never breaks the details response.
        PlayerProgressSummaryDto? progress = null;
        try
        {
            progress = await progressService.GetSummaryAsync(player.Id, cancellationToken);
        }
        catch (Exception progressEx)
        {
            // Progress summary is additive; details must still render without it.
        }

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
            player.CreatedAt,
            last30Days,
            recentActivity.Select(ToActivityDto).ToList())
        {
            Progress = progress,
            LastSeenOnSite = player.LastSeenOnSite,
            GameSessions = gameSessions
        };
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

        // The monitor keeps its roster in memory to avoid querying the database
        // every cycle; tell it the roster changed instead of waiting for the TTL.
        monitorRunner.InvalidateRoster();
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
        monitorRunner.InvalidateRoster();
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

        // Manual adjustments change real playtime days, so streaks and
        // achievements must be re-derived from the corrected data.
        try
        {
            await progressService.UpdatePlayerProgressAsync(playerId, cancellationToken);
        }
        catch (Exception progressEx)
        {
            // Progress tracking must never break the adjustment itself.
        }

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

    public async Task<PlayerDetailsDto?> UpdateClubAsync(int playerId, string club, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.GetByIdAsync(playerId, cancellationToken: cancellationToken);
        if (player is null)
        {
            return null;
        }

        player.Club = club;
        player.UpdatedAt = DateTime.UtcNow;
        await playerRepository.SaveChangesAsync(cancellationToken);
        return await GetPlayerDetailsAsync(playerId, cancellationToken);
    }

    /// <summary>
    /// Bulk sync Discord IDs to players based on Roblox user ID.
    /// Used to sync Discord links from a source database (e.g. SQLite) to the
    /// production database (e.g. PostgreSQL) when they get out of sync.
    /// </summary>
    public async Task<SyncResult> SyncDiscordIdsAsync(List<DiscordIdMapping> mappings, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult();

        foreach (var mapping in mappings)
        {
            try
            {
                var player = await playerRepository.GetByRobloxUserIdAsync(mapping.RobloxUserId, cancellationToken);
                if (player is null)
                {
                    result.Failed.Add($"RobloxUserId {mapping.RobloxUserId}: player not found");
                    continue;
                }

                var normalizedDiscordId = mapping.DiscordUserId.Trim();

                // If the Discord ID is already correctly linked, skip
                if (string.Equals(player.DiscordUserId, normalizedDiscordId, StringComparison.Ordinal))
                {
                    result.Skipped.Add($"RobloxUserId {mapping.RobloxUserId}: already linked to {normalizedDiscordId}");
                    continue;
                }

                // If this Discord ID is linked to a different player, report conflict
                var existingPlayerWithSameDiscord = await playerRepository.GetByDiscordUserIdAsync(normalizedDiscordId, cancellationToken);
                if (existingPlayerWithSameDiscord is not null && existingPlayerWithSameDiscord.Id != player.Id)
                {
                    result.Conflicts.Add($"RobloxUserId {mapping.RobloxUserId}: Discord ID {normalizedDiscordId} already linked to player {existingPlayerWithSameDiscord.Id} ({existingPlayerWithSameDiscord.Username})");
                    continue;
                }

                // Unlink the old Discord ID if it was linked to a different player
                if (existingPlayerWithSameDiscord is not null && existingPlayerWithSameDiscord.Id != player.Id)
                {
                    existingPlayerWithSameDiscord.DiscordUserId = null;
                    existingPlayerWithSameDiscord.UpdatedAt = DateTime.UtcNow;
                }

                player.DiscordUserId = normalizedDiscordId;
                player.UpdatedAt = DateTime.UtcNow;
                result.Synced.Add($"RobloxUserId {mapping.RobloxUserId} ({player.Username}) -> Discord ID {normalizedDiscordId}");
            }
            catch (Exception ex)
            {
                result.Failed.Add($"RobloxUserId {mapping.RobloxUserId}: {ex.Message}");
            }
        }

        await playerRepository.SaveChangesAsync(cancellationToken);
        return result;
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

    /// <summary>
    /// Pairs Started/Stopped activity events into per-game sessions and resolves
    /// each game's icon. A player can only be in one game at a time, so every
    /// Stopped pairs with the nearest preceding Started. Game name comes from the
    /// event's GameName column (new rows) or is parsed from the legacy message
    /// text; the icon comes from the real Roblox thumbnails API via the place ID.
    /// </summary>
    private async Task<List<GameSessionDto>> BuildGameSessionsAsync(
        List<PlayerActivityEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return new List<GameSessionDto>();
        }

        var targetGame = options.CurrentValue.TargetGameName;
        var sessions = new List<GameSessionDto>();
        GameSessionDto? pending = null;

        foreach (var ev in events.OrderBy(e => e.OccurredAt))
        {
            var isStarted = string.Equals(ev.EventType, "Started", StringComparison.OrdinalIgnoreCase);
            var isStopped = string.Equals(ev.EventType, "Stopped", StringComparison.OrdinalIgnoreCase);
            if (!isStarted && !isStopped)
            {
                continue; // manual adjustments etc. are not game sessions
            }

            var gameName = ResolveGameName(ev, targetGame);
            if (isStarted)
            {
                if (pending is not null)
                {
                    // A previous Started never got its Stopped within this feed.
                    sessions.Add(pending);
                }
                pending = new GameSessionDto
                {
                    GameName = gameName,
                    PlaceId = ev.PlaceId,
                    StartedAt = ev.OccurredAt,
                    EndedAt = null
                };
            }
            else if (pending is not null)
            {
                pending.EndedAt = ev.OccurredAt;
                pending.PlaceId ??= ev.PlaceId;
                sessions.Add(pending);
                pending = null;
            }
            else
            {
                // Stopped without its Started (feed cut-off or legacy data).
                sessions.Add(new GameSessionDto
                {
                    GameName = gameName,
                    PlaceId = ev.PlaceId,
                    StartedAt = null,
                    EndedAt = ev.OccurredAt
                });
            }
        }

        if (pending is not null)
        {
            sessions.Add(pending); // still in game (its Stopped is beyond the feed)
        }

        // Resolve real game metadata for the distinct recorded places.
        var infoByPlace = new Dictionary<long, RobloxGameInfo>();
        foreach (var placeId in sessions
            .Where(s => s.PlaceId is not null)
            .Select(s => s.PlaceId!.Value)
            .Distinct())
        {
            var info = await gameInfoClient.GetByPlaceIdAsync(placeId, cancellationToken);
            if (info is not null)
            {
                infoByPlace[placeId] = info;
            }
        }

        foreach (var session in sessions)
        {
            if (session.PlaceId is not null && infoByPlace.TryGetValue(session.PlaceId.Value, out var info))
            {
                session.GameIconUrl = info.IconUrl;
                if (!string.Equals(session.GameName, info.Name, StringComparison.OrdinalIgnoreCase))
                {
                    session.GameName = info.Name; // canonical game name from Roblox
                }
            }
            else
            {
                // Legacy rows have no place ID - still attach an icon when the
                // parsed name matches a game resolved elsewhere in this feed
                // (compared with update-prefixes like "[UPD..]" stripped).
                var byName = infoByPlace.Values
                    .FirstOrDefault(i => string.Equals(NormalizeGameName(i.Name), NormalizeGameName(session.GameName), StringComparison.OrdinalIgnoreCase));
                if (byName is not null)
                {
                    session.GameIconUrl = byName.IconUrl;
                }
            }

            // Display name without update-prefix decorations ("[UPD x] Game" -> "Game").
            session.GameName = NormalizeGameName(session.GameName);
        }

        return sessions
            .OrderByDescending(s => s.EndedAt ?? s.StartedAt ?? DateTime.MinValue)
            .Take(10)
            .ToList();
    }

    private static string ResolveGameName(PlayerActivityEvent ev, string targetGame)
    {
        if (!string.IsNullOrWhiteSpace(ev.GameName))
        {
            return ev.GameName;
        }

        // Legacy rows have no GameName: parse "Started playing X." / "Left X.";
        // adjustments and other event types fall back to the target game.
        var message = ev.Message ?? string.Empty;
        if (message.StartsWith("Started playing ", StringComparison.Ordinal))
        {
            var name = message["Started playing ".Length..].TrimEnd('.');
            if (name.Length > 0)
            {
                return name;
            }
        }
        if (message.StartsWith("Left ", StringComparison.Ordinal))
        {
            var name = message["Left ".Length..].TrimEnd('.');
            if (name.Length > 0)
            {
                return name;
            }
        }
        return targetGame;
    }

    /// <summary>
    /// Strips Roblox live-update decorations from displayed game names, e.g.
    /// "[UPD x] Racket Rivals " becomes "Racket Rivals" and an emoji-prefixed
    /// Blox Fruits name loses its bracketed emoji tag.
    /// </summary>
    public static string NormalizeGameName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Unknown game";
        }

        var trimmed = name.Trim();
        while (trimmed.StartsWith('['))
        {
            var close = trimmed.IndexOf(']');
            if (close < 0)
            {
                break;
            }
            trimmed = trimmed[(close + 1)..].TrimStart();
        }

        return trimmed.Length > 0 ? trimmed : name.Trim();
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
}
