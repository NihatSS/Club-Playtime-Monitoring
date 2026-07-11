namespace ClubPlaytime.Api.Services;

public interface IPlayerMonitorRunner
{
    Task<MonitorRunResult> CheckAllPlayersAsync(CancellationToken cancellationToken = default);
}
