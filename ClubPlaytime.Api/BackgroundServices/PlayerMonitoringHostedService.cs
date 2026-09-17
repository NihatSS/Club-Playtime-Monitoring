using ClubPlaytime.Api.Options;
using ClubPlaytime.Api.Services;
using Microsoft.Extensions.Options;

namespace ClubPlaytime.Api.BackgroundServices;

public sealed class PlayerMonitoringHostedService(
    IPlayerMonitorRunner monitorRunner,
    IOptionsMonitor<MonitoringOptions> options,
    ILogger<PlayerMonitoringHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Player monitoring background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await monitorRunner.CheckAllPlayersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled monitoring error. The worker will retry on the next interval.");
            }

            var intervalSeconds = options.CurrentValue.CheckIntervalSeconds;
            // While degraded (DB unreachable), back off to 5 minutes: the retry
            // loop in Program.cs owns recovery; the monitor only needs a slow
            // heartbeat until the database is back.
            if (!Services.RunnerGate.IsDatabaseReady)
            {
                intervalSeconds = 300;
            }
            intervalSeconds = Math.Max(10, intervalSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
