using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Repositories;

public sealed class PlayerRepository(ClubPlaytimeDbContext dbContext) : IPlayerRepository
{
    public Task<List<Player>> GetAllAsync(bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Players.AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.OrderBy(player => player.Username).ToListAsync(cancellationToken);
    }

    public Task<Player?> GetByIdAsync(int id, bool trackChanges = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Players.AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(player => player.Id == id, cancellationToken);
    }

    public Task<Player?> GetByRobloxUserIdAsync(long robloxUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.Players.FirstOrDefaultAsync(player => player.RobloxUserId == robloxUserId, cancellationToken);
    }

    public Task<Player?> GetByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.Players.FirstOrDefaultAsync(player => player.DiscordUserId == discordUserId, cancellationToken);
    }

    public async Task<Player?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // Use case-insensitive comparison via EF Core translation.
        // SQLite uses case-insensitive ASCII comparison by default for LIKE/equals.
        var normalized = (username ?? string.Empty).Trim();
        return await dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Username == normalized, cancellationToken);
    }

    public Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        return dbContext.Players.AddAsync(player, cancellationToken).AsTask();
    }

    public void Remove(Player player)
    {
        dbContext.Players.Remove(player);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
