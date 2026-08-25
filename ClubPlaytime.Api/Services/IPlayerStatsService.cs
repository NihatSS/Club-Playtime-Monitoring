using ClubPlaytime.Api.DTOs;

namespace ClubPlaytime.Api.Services;

public interface IPlayerStatsService
{
    Task<IReadOnlyList<PlayerDto>> GetPlayersAsync(CancellationToken cancellationToken = default);

    Task<PlayerDetailsDto?> GetPlayerDetailsAsync(int playerId, CancellationToken cancellationToken = default);

    Task<PlayerDto> AddPlayerAsync(AddPlayerRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeletePlayerAsync(int playerId, CancellationToken cancellationToken = default);

    Task<PlayerDetailsDto?> AdjustPlaytimeAsync(int playerId, AdjustPlaytimeRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardPlayerDto>> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeeklyLeaderboardDto>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardPlayerDto>> GetLeaderboardAsync(string period, CancellationToken cancellationToken = default);

    Task<PlayerDetailsDto?> GetPlayerByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken = default);

    Task<PlayerDetailsDto?> LinkDiscordUserAsync(string robloxUsername, string discordUserId, CancellationToken cancellationToken = default);

    Task<PlayerDetailsDto?> UpdateClubAsync(int playerId, string club, CancellationToken cancellationToken = default);

    Task<string> ExportCsvAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
