using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Repositories;

public interface IActivityRepository
{
    Task AddAsync(PlayerActivityEvent activityEvent, CancellationToken cancellationToken = default);

    Task<List<PlayerActivityEvent>> GetRecentForPlayerAsync(int playerId, int take, CancellationToken cancellationToken = default);
}
