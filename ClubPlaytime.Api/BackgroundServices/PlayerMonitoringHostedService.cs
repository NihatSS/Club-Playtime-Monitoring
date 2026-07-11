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

            var intervalSeconds = Math.Max(10, options.CurrentValue.CheckIntervalSeconds);
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
