using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Repositories;

public interface IDailyPlaytimeRepository
{
    Task<DailyPlaytime> GetOrCreateAsync(int playerId, DateOnly date, CancellationToken cancellationToken = default);

    Task<Dictionary<int, long>> GetPlaySecondsForDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<List<DailyPlaytime>> GetRangeAsync(int playerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<Dictionary<int, long>> GetPlaySecondsSinceAsync(DateOnly from, CancellationToken cancellationToken = default);
}
