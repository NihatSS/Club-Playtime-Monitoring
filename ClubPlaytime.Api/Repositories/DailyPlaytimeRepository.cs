using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Repositories;

public sealed class DailyPlaytimeRepository(ClubPlaytimeDbContext dbContext) : IDailyPlaytimeRepository
{
    public async Task<DailyPlaytime> GetOrCreateAsync(int playerId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var dailyPlaytime = await dbContext.DailyPlaytime
            .FirstOrDefaultAsync(day => day.PlayerId == playerId && day.Date == date, cancellationToken);

        if (dailyPlaytime is not null)
        {
            return dailyPlaytime;
        }

        dailyPlaytime = new DailyPlaytime
        {
            PlayerId = playerId,
            Date = date,
            PlaySeconds = 0
        };

        await dbContext.DailyPlaytime.AddAsync(dailyPlaytime, cancellationToken);
        return dailyPlaytime;
    }

    public Task<Dictionary<int, long>> GetPlaySecondsForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return dbContext.DailyPlaytime
            .AsNoTracking()
            .Where(day => day.Date == date)
            .GroupBy(day => day.PlayerId)
            .ToDictionaryAsync(group => group.Key, group => group.Sum(day => day.PlaySeconds), cancellationToken);
    }

    public Task<List<DailyPlaytime>> GetRangeAsync(int playerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return dbContext.DailyPlaytime
            .AsNoTracking()
            .Where(day => day.PlayerId == playerId && day.Date >= from && day.Date <= to)
            .OrderBy(day => day.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<Dictionary<int, long>> GetPlaySecondsSinceAsync(DateOnly from, CancellationToken cancellationToken = default)
    {
        return dbContext.DailyPlaytime
            .AsNoTracking()
            .Where(day => day.Date >= from)
            .GroupBy(day => day.PlayerId)
            .ToDictionaryAsync(group => group.Key, group => group.Sum(day => day.PlaySeconds), cancellationToken);
    }
}
