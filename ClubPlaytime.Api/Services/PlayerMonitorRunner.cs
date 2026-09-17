using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Repositories;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

/// <summary>
/// Shared switches between host and monitor loop. When the database is
/// unreachable (hosted Postgres limit stop, cold wake), the monitor pauses
/// polling so it doesn't burn requests against a stopped database or flood the
/// logs.
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

    // Last successful presence sample per Roblox user id. Comparing the fresh
    // Roblox response against this is what lets a quiet scan skip the database
    // entirely: nobody is in the target game and no player changed state, so
    // there is literally nothing to write.
    //
    // This matters for cost, not just speed. Hosted serverless Postgres bills
    // only while the database is awake and suspends after a few idle minutes, so
    // a tracker that queries every 60 seconds forever keeps the project running
    // 24/7 — ~720 compute-hours a month against a free plan that includes ~192.
    // With the check below, the database is only woken by real activity.
    private readonly Dictionary<long, PresenceSnapshot> _lastPresence = new();

    // Player roster cached in memory: re-reading Players every cycle would keep
    // the database awake 24/7 again, which is the problem we are avoiding.
    private List<Player> _roster = [];
    private DateTime _rosterLoadedAtUtc = DateTime.MinValue;

    // Quiet cycles log nothing (the default level is Information), so emit one
    // heartbeat line an hour to make it obvious the monitor is still running.
    private DateTime _lastQuietHeartbeatUtc = DateTime.MinValue;

    public Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken = default)
        => CheckAllPlayersAsync(cancellationToken, forceRosterRefresh: false);

    public async Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken, bool forceRosterRefresh)
    {
        // Database unavailable (limit stop / cold wake): skip the scan entirely
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
            using var scope = scopeFactory.CreateScope();
            var playerRepository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
            var presenceClient = scope.ServiceProvider.GetRequiredService<IRobloxPresenceClient>();

            var roster = await GetRosterAsync(playerRepository, forceRosterRefresh, cancellationToken);
            if (roster.Count == 0)
            {
                logger.LogInformation("No players to check.");
                return new MonitorRunResult(0, 0, 0, 0, false);
            }

            // Step 1: Roblox only. This call costs nothing in database terms, so
            // it decides whether step 2 (the database work) is needed at all.
            IReadOnlyDictionary<long, RobloxPresenceResult> presence =
                await presenceClient.GetPresenceBatchAsync(roster, cancellationToken);

            var assessment = AssessScan(roster, presence);
            if (!assessment.NeedsDatabase)
            {
                LogQuietScan(roster.Count, assessment);
                return new MonitorRunResult(roster.Count, 0, assessment.Offline, assessment.Errors, false);
            }

            // Step 2: something happened (someone is in the target game, or a
            // player's online/game state changed) — pay for the database work.
            var dailyPlaytimeRepository = scope.ServiceProvider.GetRequiredService<IDailyPlaytimeRepository>();
            var activityRepository = scope.ServiceProvider.GetRequiredService<IActivityRepository>();
            var discordNotifier = scope.ServiceProvider.GetRequiredService<IDiscordNotifier>();
            var progressService = scope.ServiceProvider.GetRequiredService<PlayerProgressService>();

            // Single scope for the whole scan: players are loaded tracked once and
            // mutated in place. The old code loaded an untracked list for the
            // presence batch, then re-fetched every player by id from the DB —
            // one redundant query per player per scan.
            var players = await playerRepository.GetAllAsync(trackChanges: true, cancellationToken);
            if (players.Count == 0)
            {
                logger.LogInformation("No players to check.");
                return new MonitorRunResult(0, 0, 0, 0, false);
            }

            // A player added since the roster was cached has no presence sample
            // yet; look those up so they aren't reported as errors.
            var missingPresence = players.Where(p => !presence.ContainsKey(p.RobloxUserId)).ToList();
            if (missingPresence.Count > 0)
            {
                var extra = await presenceClient.GetPresenceBatchAsync(missingPresence, cancellationToken);
                presence = presence
                    .Concat(extra)
                    .GroupBy(entry => entry.Key)
                    .ToDictionary(group => group.Key, group => group.Last().Value);
            }

            logger.LogInformation("Checking {PlayerCount} players (activity detected).", players.Count);

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
                    presence.GetValueOrDefault(player.RobloxUserId),
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

            // Deferred non-target-game "Stopped" rows are added before the bulk
            // save so each session lands as a Started/Stopped pair. They used to
            // be collected and then dropped here, which left the website with
            // sessions that never ended.
            foreach (var otherGameEvent in otherGameEvents)
            {
                await activityRepository.AddAsync(otherGameEvent, cancellationToken);
            }

            // One bulk save for the whole scan instead of one write per player.
            // EF skips players whose tracked values didn't change, so a scan that
            // only flipped IsOnline still costs a single round trip.
            await playerRepository.SaveChangesAsync(cancellationToken);

            // Only after the write succeeded: the snapshot is the monitor's memory
            // of what the database now reflects.
            RememberPresence(players, presence);

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
    /// Drops the cached roster so the next scan re-reads the players table.
    /// Called when players are added or removed, and by the admin "check now".
    /// </summary>
    public void InvalidateRoster()
    {
        _rosterLoadedAtUtc = DateTime.MinValue;
    }

    private async Task<List<Player>> GetRosterAsync(
        IPlayerRepository playerRepository,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(30, options.CurrentValue.RosterCacheSeconds));
        if (!forceRefresh
            && _rosterLoadedAtUtc != DateTime.MinValue
            && DateTime.UtcNow - _rosterLoadedAtUtc < ttl)
        {
            return _roster;
        }

        var players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
        _roster = players;
        _rosterLoadedAtUtc = DateTime.UtcNow;

        // Forget snapshot entries for players that no longer exist so the
        // dictionary can't grow across roster changes.
        var liveRobloxIds = players.Select(p => p.RobloxUserId).ToHashSet();
        foreach (var staleRobloxId in _lastPresence.Keys.Where(id => !liveRobloxIds.Contains(id)).ToList())
        {
            _lastPresence.Remove(staleRobloxId);
        }

        return _roster;
    }

    /// <summary>
    /// The presence-only half of a scan. Decides whether the database work is
    /// worth doing: a target-game player always is (playtime accrues per check),
    /// and any online/game transition is (the website's presence and activity
    /// feed changed). Everything else is a quiet scan.
    /// </summary>
    private ScanAssessment AssessScan(
        IReadOnlyList<Player> roster,
        IReadOnlyDictionary<long, RobloxPresenceResult> presence)
    {
        var playing = 0;
        var online = 0;
        var offline = 0;
        var errors = 0;
        var needsDatabase = false;

        foreach (var player in roster)
        {
            if (!presence.TryGetValue(player.RobloxUserId, out var current) || !current.IsSuccessful)
            {
                errors++;
                // A failed sample is not a state change: the previous snapshot is
                // kept, so the transition is still detected (and playtime still
                // capped the same way) once Roblox answers again.
                continue;
            }

            if (current.IsPlayingTargetGame)
            {
                playing++;
                needsDatabase = true;
                continue;
            }

            if (current.IsOnline)
            {
                online++;
            }
            else
            {
                offline++;
            }

            if (!_lastPresence.TryGetValue(player.RobloxUserId, out var previous)
                || previous.IsOnline != current.IsOnline
                || previous.IsPlayingTargetGame != current.IsPlayingTargetGame
                || !string.Equals(previous.CurrentGame, current.CurrentGame, StringComparison.Ordinal))
            {
                needsDatabase = true;
            }
        }

        return new ScanAssessment(needsDatabase, playing, online, offline, errors);
    }

    private void RememberPresence(
        IReadOnlyList<Player> players,
        IReadOnlyDictionary<long, RobloxPresenceResult> presence)
    {
        foreach (var player in players)
        {
            if (presence.TryGetValue(player.RobloxUserId, out var current) && current.IsSuccessful)
            {
                _lastPresence[player.RobloxUserId] = new PresenceSnapshot(
                    current.IsOnline,
                    current.IsPlayingTargetGame,
                    current.CurrentGame);
            }
        }
    }

    private void LogQuietScan(int checkedPlayers, ScanAssessment assessment)
    {
        if (assessment.Errors > 0)
        {
            logger.LogWarning("{Errors} of {Checked} Roblox presence lookups failed this cycle; nothing to record.",
                assessment.Errors, checkedPlayers);
        }

        // One line an hour: enough to prove the monitor is alive without turning
        // every quiet cycle into log volume.
        if (DateTime.UtcNow - _lastQuietHeartbeatUtc < TimeSpan.FromHours(1))
        {
            return;
        }

        _lastQuietHeartbeatUtc = DateTime.UtcNow;
        logger.LogInformation(
            "Quiet scan: {Checked} players checked, no target-game playtime and no state changes (database untouched).",
            checkedPlayers);
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

    private readonly record struct PresenceSnapshot(bool IsOnline, bool IsPlayingTargetGame, string? CurrentGame);

    private readonly record struct ScanAssessment(
        bool NeedsDatabase,
        int Playing,
        int Online,
        int Offline,
        int Errors);

    private enum PlayerCheckOutcome
    {
        Playing,
        Online,
        Offline,
        Error
    }
}
