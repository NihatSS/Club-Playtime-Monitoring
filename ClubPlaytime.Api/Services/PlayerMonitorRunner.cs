using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Repositories;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.Services;

public sealed class PlayerMonitorRunner(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<MonitoringOptions> options,
    ILogger<PlayerMonitorRunner> logger) : IPlayerMonitorRunner
{
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    public async Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken = default)
    {
        if (!await _scanLock.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Monitoring scan skipped because another scan is already running.");
            return new MonitorRunResult(0, 0, 0, 0, true);
        }

        try
        {
            List<Player> players;
            IRobloxPresenceClient presenceClient;
            using (var scope = scopeFactory.CreateScope())
            {
                var playerRepository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
                players = await playerRepository.GetAllAsync(trackChanges: false, cancellationToken);
                presenceClient = scope.ServiceProvider.GetRequiredService<IRobloxPresenceClient>();
            }

            if (players.Count == 0)
            {
                logger.LogInformation("No players to check.");
                return new MonitorRunResult(0, 0, 0, 0, false);
            }

            logger.LogInformation("Checking {PlayerCount} players...", players.Count);

            // Batch all presence checks into a single API call
            var presenceResults = await presenceClient.GetPresenceBatchAsync(players, cancellationToken);

            var playingCount = 0;
            var onlineCount = 0;
            var offlineCount = 0;
            var errorCount = 0;

            // Reuse a single scope for all player updates instead of creating one per player
            using var updateScope = scopeFactory.CreateScope();
            var updatePlayerRepo = updateScope.ServiceProvider.GetRequiredService<IPlayerRepository>();
            var updateDailyRepo = updateScope.ServiceProvider.GetRequiredService<IDailyPlaytimeRepository>();
            var updateActivityRepo = updateScope.ServiceProvider.GetRequiredService<IActivityRepository>();
            var updateDiscordNotifier = updateScope.ServiceProvider.GetRequiredService<IDiscordNotifier>();
            var updateProgressService = updateScope.ServiceProvider.GetRequiredService<PlayerProgressService>();

            // Non-target-game Started/Stopped events deferred to the end of the scan
            // so the bulk SaveChangesAsync happens once, after the per-player loop.
            var otherGameEvents = new List<PlayerActivityEvent>();

            foreach (var player in players)
            {
                var outcome = await ApplyPresenceResultAsync(
                    updatePlayerRepo,
                    updateDailyRepo,
                    updateActivityRepo,
                    updateDiscordNotifier,
                    player.Id,
                    presenceResults.GetValueOrDefault(player.RobloxUserId),
                    otherGameEvents,
                    cancellationToken);

                // Playtime was recorded for this player this scan — refresh their
                // streak and achievements from the new real data. Skipped for
                // players with no new playtime so idle players cost nothing.
                if (outcome == PlayerCheckOutcome.Playing)
                {
                    try
                    {
                        await updateProgressService.UpdatePlayerProgressAsync(player.Id, cancellationToken);
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

            // Bulk-insert deferred non-target-game events (added by ApplyPresenceResultAsync).
            foreach (var otherGameEvent in otherGameEvents)
            {
                await updateActivityRepo.AddAsync(otherGameEvent, cancellationToken);
            }
            if (otherGameEvents.Count > 0)
            {
                await updatePlayerRepo.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Completed. {Playing} playing, {Online} online, {Offline} offline, {Errors} errors.",
                playingCount, onlineCount, offlineCount, errorCount);
            return new MonitorRunResult(players.Count, playingCount, offlineCount, errorCount, false);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private async Task<PlayerCheckOutcome> ApplyPresenceResultAsync(
        IPlayerRepository playerRepository,
        IDailyPlaytimeRepository dailyPlaytimeRepository,
        IActivityRepository activityRepository,
        IDiscordNotifier discordNotifier,
        int playerId,
        RobloxPresenceResult? presence,
        List<PlayerActivityEvent> otherGameEvents,
        CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetByIdAsync(playerId, cancellationToken: cancellationToken);

        if (player is null)
        {
            return PlayerCheckOutcome.Error;
        }

        if (presence is null || !presence.IsSuccessful)
        {
            logger.LogWarning("{Username} -> Roblox check failed: {ErrorMessage}",
                player.Username, presence?.ErrorMessage ?? "No presence data");
            return PlayerCheckOutcome.Error;
        }

        var now = DateTime.UtcNow;
        var utcDate = DateOnly.FromDateTime(now);
        var wasPlaying = player.LastSeenPlaying.HasValue;
        var targetGame = options.CurrentValue.TargetGameName;

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
            }

            player.IsOnline = true;
            player.CurrentlyPlaying = presence.CurrentGame ?? targetGame;
            player.LastSeenPlaying = now;
            player.UpdatedAt = now;
            await playerRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{Username} -> Playing {GameName}{Elapsed}",
                player.Username,
                player.CurrentlyPlaying,
                elapsedSeconds > 0 ? $" +{elapsedSeconds} seconds" : string.Empty);

            return PlayerCheckOutcome.Playing;
        }

        player.IsOnline = presence.IsOnline;
        player.CurrentlyPlaying = presence.IsOnline ? presence.CurrentGame : null;
        // Reset LastSeenPlaying so rejoining the game doesn't count the offline gap as playtime.
        // "Last seen" info is preserved in PlayerActivityEvent entries (the Stopped event).
        player.LastSeenPlaying = null;
        player.UpdatedAt = now;

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

        await playerRepository.SaveChangesAsync(cancellationToken);
        return presence.IsOnline ? PlayerCheckOutcome.Online : PlayerCheckOutcome.Offline;
    }

    private enum PlayerCheckOutcome
    {
        Playing,
        Online,
        Offline,
        Error
    }
}
