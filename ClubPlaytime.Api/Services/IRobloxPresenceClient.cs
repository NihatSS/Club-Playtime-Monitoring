using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Services;

public interface IRobloxPresenceClient
{
    Task<RobloxPresenceResult> GetPresenceAsync(Player player, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<long, RobloxPresenceResult>> GetPresenceBatchAsync(
        IReadOnlyList<Player> players,
        CancellationToken cancellationToken = default);
}
