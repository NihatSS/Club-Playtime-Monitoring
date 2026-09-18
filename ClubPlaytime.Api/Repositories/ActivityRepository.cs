using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Repositories;

public sealed class ActivityRepository(ClubPlaytimeDbContext dbContext) : IActivityRepository
{
    public Task AddAsync(PlayerActivityEvent activityEvent, CancellationToken cancellationToken = default)
    {
        return dbContext.PlayerActivityEvents.AddAsync(activityEvent, cancellationToken).AsTask();
    }

    public async Task<Dictionary<int, DateTime>> GetLastGameSeenAsync(CancellationToken cancellationToken = default)
    {
        // Started/Stopped are the two event types the monitor writes for target-game
        // presence; "Adjusted" rows are admin playtime edits and say nothing about
        // when the player was last seen.
        var rows = await dbContext.PlayerActivityEvents
            .AsNoTracking()
            .Where(activity => activity.EventType == "Started" || activity.EventType == "Stopped")
            .GroupBy(activity => activity.PlayerId)
            .Select(group => new { PlayerId = group.Key, LastSeen = group.Max(activity => activity.OccurredAt) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.PlayerId, row => row.LastSeen);
    }

    public Task<List<PlayerActivityEvent>> GetRecentForPlayerAsync(int playerId, int take, CancellationToken cancellationToken = default)
    {
        return dbContext.PlayerActivityEvents
            .AsNoTracking()
            .Where(activity => activity.PlayerId == playerId)
            .OrderByDescending(activity => activity.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
