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
        var normalizedDiscordUserId = (discordUserId ?? string.Empty).Trim();
        if (normalizedDiscordUserId.Length == 0)
        {
            return Task.FromResult<Player?>(null);
        }

        // Tracker players pre-date the website-account system, so the primary
        // Discord link lives on Players.  Accounts created/linked through the
        // newer website flow can also hold the Discord ID on Users.  Looking up
        // only Players made a correctly linked website account appear missing to
        // the Discord bot.
        return GetByDiscordUserIdCoreAsync(normalizedDiscordUserId, cancellationToken);
    }

    private async Task<Player?> GetByDiscordUserIdCoreAsync(string discordUserId, CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .FirstOrDefaultAsync(p => p.DiscordUserId == discordUserId, cancellationToken);

        if (player is not null)
        {
            return player;
        }

        return await dbContext.Users
            .Where(u => u.DiscordUserId == discordUserId && u.PlayerId != null)
            .Select(u => u.Player)
            .FirstOrDefaultAsync(cancellationToken);
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
