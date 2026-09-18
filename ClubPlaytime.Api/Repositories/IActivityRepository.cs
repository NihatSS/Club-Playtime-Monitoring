using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Repositories;

public interface IActivityRepository
{
    Task AddAsync(PlayerActivityEvent activityEvent, CancellationToken cancellationToken = default);

    Task<List<PlayerActivityEvent>> GetRecentForPlayerAsync(int playerId, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Latest target-game presence time for every player that has one (Started /
    /// Stopped events), keyed by player id. Feeds the "last seen" values shown
    /// on the dashboard, which the monitor cannot supply itself: it clears the
    /// accrual cursor when a session ends, so the event feed is the only record
    /// of when a player was last in game. One grouped query, not one per player.
    /// </summary>
    Task<Dictionary<int, DateTime>> GetLastGameSeenAsync(CancellationToken cancellationToken = default);
}
