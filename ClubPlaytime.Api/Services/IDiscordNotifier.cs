using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Services;

public interface IDiscordNotifier
{
    Task PlayerStartedAsync(Player player, string gameName, CancellationToken cancellationToken = default);

    Task PlayerStoppedAsync(Player player, string gameName, CancellationToken cancellationToken = default);
}
