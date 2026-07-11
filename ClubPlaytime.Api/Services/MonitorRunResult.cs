namespace ClubPlaytime.Api.Services;

public sealed record MonitorRunResult(
    int CheckedPlayers,
    int PlayingPlayers,
    int OfflinePlayers,
    int ErrorCount,
    bool Skipped);
