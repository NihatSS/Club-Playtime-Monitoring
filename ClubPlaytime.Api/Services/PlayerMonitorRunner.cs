using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Repositories;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

/// <summary>
/// Shared switches between host and monitor loop. When the database is
/// unreachable (Neon quota stop, cold wake), the monitor pauses polling so it
/// doesn't burn requests against a stopped database or flood the logs.
/// </summary>
public static class RunnerGate
{
    /// <summary>Set by Program.cs; returns true when the DB is initialized and usable.</summary>
    public static Func<bool>? DatabaseReady { get; set; }

    public static bool IsDatabaseReady => DatabaseReady?.Invoke() ?? true;
}

public sealed class PlayerMonitorRunner(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<MonitoringOptions> options,
    ILogger<PlayerMonitorRunner> logger) : IPlayerMonitorRunner
{
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    public async Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken = default)
    {
        // Database unavailable (quota stop / cold wake): skip the scan entirely
        // instead of throwing against a stopped DB every interval.
        if (!RunnerGate.IsDatabaseReady)
        {
            return new MonitorRunResult(0, 0, 0, 0, true);
        }

        if (!await _scanLock.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Monitoring scan skipped because another scan is running.");
            return new MonitorRunResult(0, 0, 0, 0, true);
        }

        try
        {
            // Single scope for the whole scan: players are loaded tracked once and
            // mutated in place. The old code loaded an untracked list for the
            // presence batch, then re-fetched every player by id from the DB —
            // one redundant query per player per scan.
            using var scope = scopeFactory.CreateScope();
            var playerRepository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
            var dailyPlaytimeRepository = scope.ServiceProvider.GetRequiredService<IDailyPlaytimeRepository>();
            var activityRepository = scope.ServiceProvider.GetRequiredService<IActivityRepository>();
            var discordNotifier = scope.ServiceProvider.GetRequiredService<IDiscordNotifier>();
            var progressService = scope.ServiceProvider.GetRequiredService<PlayerProgressService>();

            var players = await playerRepository.GetAllAsync(trackChanges: true, cancellationToken);
            if (players.Count == 0)
            {
                logger.LogInformation("No players to check.");
                return new MonitorRunResult(0, 0, 0, 0, false);
            }

            logger.LogInformation("Checking {PlayerCount} players...", players.Count);

            // Batch all presence checks into a single API call.
            var presenceClient = scope.ServiceProvider.GetRequiredService<IRobloxPresenceClient>();
            var presenceResults = await presenceClient.GetPresenceBatchAsync(players, cancellationToken);

            var playingCount = 0;
            var onlineCount = 0;
            var offlineCount = 0;
            var errorCount = 0;

            // Non-target-game Started/Stopped events deferred to the end of the scan
            // so the bulk SaveChangesAsync happens once, after the per-player loop.
            var otherGameEvents = new List<PlayerActivityEvent>();

            foreach (var player in players)
            {
                var (outcome, secondsRecorded, progressDirty) = await ApplyPresenceResultAsync(
                    dailyPlaytimeRepository,
                    activityRepository,
                    discordNotifier,
                    player,
                    presenceResults.GetValueOrDefault(player.RobloxUserId),
                    otherGameEvents,
                    cancellationToken);

                // Refresh streak/achievements only when real new playtime was
                // recorded — idle players and state-flip scans cost nothing.
                // (Admin edits can also change derived data; those endpoints
                // recompute progress themselves.)
                if (secondsRecorded > 0 || progressDirty)
                {
                    try
                    {
                        await progressService.UpdatePlayerProgressAsync(player.Id, cancellationToken);
                    }
                    catch (Exception progressEx)
                    {
                        // Progress tracking must never break playtime tracking.
                        logger.LogWarning(progressEx, "Progress update failed for {Username}", player.Username);
                    }
                }

                switch (outcome)
                {
                    case PlayerCheckOutcome.Playing:
                        playingCount++;
                        break;
                    case PlayerCheckOutcome.Online:
                        onlineCount++;
                        break;
                    case PlayerCheckOutcome.Offline:
                        offlineCount++;
                        break;
                    case PlayerCheckOutcome.Error:
                        errorCount++;
                        break;
                }
            }

            // One bulk save for the whole scan instead of one write per player.
            // EF skips players whose tracked values didn't change, so a quiet scan
            // (everyone still playing/offline, nothing new) costs zero DB writes.
            await playerRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Completed. {Playing} playing, {Online} online, {Offline} offline, {Errors} errors.",
                playingCount, onlineCount, offlineCount, errorCount);
            return new MonitorRunResult(players.Count, playingCount, offlineCount, errorCount, false);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    /// <summary>
    /// Applies one presence result to a tracked player entity.
    /// Returns the outcome, how many seconds of playtime were recorded this scan,
    /// and whether derived progress data should be refreshed.
    /// </summary>
    private async Task<(PlayerCheckOutcome Outcome, long SecondsRecorded, bool ProgressDirty)> ApplyPresenceResultAsync(
        IDailyPlaytimeRepository dailyPlaytimeRepository,
        IActivityRepository activityRepository,
        IDiscordNotifier discordNotifier,
        Player player,
        RobloxPresenceResult? presence,
        List<PlayerActivityEvent> otherGameEvents,
        CancellationToken cancellationToken)
    {
        if (presence is null || !presence.IsSuccessful)
        {
            logger.LogWarning("{Username} -> Roblox check failed: {ErrorMessage}",
                player.Username, presence?.ErrorMessage ?? "No presence data");
            return (PlayerCheckOutcome.Error, 0, false);
        }

        var now = DateTime.UtcNow;
        var utcDate = DateOnly.FromDateTime(now);
        var wasPlaying = player.LastSeenPlaying.HasValue;
        var targetGame = options.CurrentValue.TargetGameName;
        var progressDirty = false;

        if (presence.IsPlayingTargetGame)
        {
            var elapsedSeconds = 0L;
            if (player.LastSeenPlaying is { } lastSeenPlaying)
            {
                var rawElapsed = (long)Math.Floor((now - lastSeenPlaying).TotalSeconds);
                // Safety cap: if the gap exceeds 3x the check interval, treat it as a new session.
                // This prevents stale LastSeenPlaying values from inflating playtime.
                var maxElapsed = options.CurrentValue.CheckIntervalSeconds * 3L;
                elapsedSeconds = Math.Clamp(rawElapsed, 0, maxElapsed);
                if (elapsedSeconds > 0)
                {
                    var today = await dailyPlaytimeRepository.GetOrCreateAsync(player.Id, utcDate, cancellationToken);
                    today.PlaySeconds += elapsedSeconds;
                    player.TotalPlaySeconds += elapsedSeconds;
                    progressDirty = true;
                }
            }
            else
            {
                await activityRepository.AddAsync(new PlayerActivityEvent
                {
                    PlayerId = player.Id,
                    EventType = "Started",
                    Message = $"Started playing {presence.CurrentGame ?? targetGame}.",
                    GameName = presence.CurrentGame ?? targetGame,
                    PlaceId = presence.PlaceId,
                    OccurredAt = now
                }, cancellationToken);

                await discordNotifier.PlayerStartedAsync(player, presence.CurrentGame ?? targetGame, cancellationToken);
                progressDirty = true;
            }

            var stateChanged = !player.IsOnline || !string.Equals(player.CurrentlyPlaying, presence.CurrentGame ?? targetGame, StringComparison.Ordinal);
            player.IsOnline = true;
            player.CurrentlyPlaying = presence.CurrentGame ?? targetGame;
            player.LastSeenPlaying = now;
            if (stateChanged)
            {
                // Only bump UpdatedAt when something observable changed — writing
                // it unconditionally turned every scan into a DB write per player.
                player.UpdatedAt = now;
            }

            logger.LogInformation("{Username} -> Playing {GameName}{Elapsed}",
                player.Username,
                player.CurrentlyPlaying,
                elapsedSeconds > 0 ? $" +{elapsedSeconds} seconds" : string.Empty);

            return (PlayerCheckOutcome.Playing, elapsedSeconds, progressDirty);
        }

        var onlineStateChanged = player.IsOnline != presence.IsOnline;
        var gameStateChanged = !string.Equals(player.CurrentlyPlaying, presence.IsOnline ? presence.CurrentGame : null, StringComparison.Ordinal);
        var lastSeenCleared = player.LastSeenPlaying.HasValue;

        player.IsOnline = presence.IsOnline;
        player.CurrentlyPlaying = presence.IsOnline ? presence.CurrentGame : null;
        // Reset LastSeenPlaying so rejoining the game doesn't count the offline gap as playtime.
        // "Last seen" info is preserved in PlayerActivityEvent entries (the Stopped event).
        player.LastSeenPlaying = null;
        if (onlineStateChanged || gameStateChanged || lastSeenCleared)
        {
            player.UpdatedAt = now;
        }

        var leftGameName = presence.CurrentGame ?? targetGame;

        if (wasPlaying)
        {
            await activityRepository.AddAsync(new PlayerActivityEvent
            {
                PlayerId = player.Id,
                EventType = "Stopped",
                Message = $"Left {leftGameName}.",
                // GameName mirrors what the matching Started event recorded so the
                // website can pair them into one session (same game name).
                GameName = leftGameName,
                OccurredAt = now
            }, cancellationToken);

            await discordNotifier.PlayerStoppedAsync(player, targetGame, cancellationToken);
            logger.LogInformation("{Username} -> Left game", player.Username);
        }
        else if (presence.IsOnline)
        {
            // Playing (or just online in) a NON-target game: the tracker counts no
            // playtime for these, but the website's Recent Activity still shows the
            // session — so record Started/Stopped events with the real game name.
            var gameName = presence.CurrentGame;
            if (string.IsNullOrWhiteSpace(gameName))
            {
                gameName = "a Roblox game";
            }

            await activityRepository.AddAsync(new PlayerActivityEvent
            {
                PlayerId = player.Id,
                EventType = "Started",
                Message = $"Started playing {gameName}.",
                GameName = gameName,
                PlaceId = presence.PlaceId,
                OccurredAt = now
            }, cancellationToken);

            otherGameEvents.Add(new PlayerActivityEvent
            {
                PlayerId = player.Id,
                EventType = "Stopped",
                Message = $"Left {gameName}.",
                GameName = gameName,
                PlaceId = presence.PlaceId,
                // A single presence sample only proves "was in that game at this
                // instant"; the next successful check is the earliest believable end.
                OccurredAt = now.AddSeconds(Math.Max(30, options.CurrentValue.CheckIntervalSeconds))
            });

            logger.LogInformation("{Username} -> Online ({GameName})", player.Username, gameName);
        }
        else
        {
            logger.LogInformation("{Username} -> Offline", player.Username);
        }

        return (presence.IsOnline ? PlayerCheckOutcome.Online : PlayerCheckOutcome.Offline, 0, progressDirty);
    }

    private enum PlayerCheckOutcome
    {
        Playing,
        Online,
        Offline,
        Error
    }
}
