namespace ClubPlaytime.Api.Services;

public interface IPlayerMonitorRunner
{
    Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks every tracked player. <paramref name="forceRosterRefresh"/> re-reads
    /// the players table instead of using the monitor's in-memory roster cache
    /// (used by the admin "check now" so a just-added player is picked up).
    /// </summary>
    Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken, bool forceRosterRefresh);

    /// <summary>
    /// Drops the cached player roster so the next scan re-reads it. Call after
    /// adding or removing players.
    /// </summary>
    void InvalidateRoster();
}
