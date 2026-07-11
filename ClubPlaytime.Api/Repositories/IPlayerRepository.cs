using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Repositories;

public interface IPlayerRepository
{
    Task<List<Player>> GetAllAsync(bool trackChanges = true, CancellationToken cancellationToken = default);

    Task<Player?> GetByIdAsync(int id, bool trackChanges = true, CancellationToken cancellationToken = default);

    Task<Player?> GetByRobloxUserIdAsync(long robloxUserId, CancellationToken cancellationToken = default);

    Task<Player?> GetByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken = default);

    Task<Player?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task AddAsync(Player player, CancellationToken cancellationToken = default);

    void Remove(Player player);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
