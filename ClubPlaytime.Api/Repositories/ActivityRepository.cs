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
