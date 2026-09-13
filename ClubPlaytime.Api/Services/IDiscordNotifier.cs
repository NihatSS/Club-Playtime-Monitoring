using ClubPlaytime.Api.Models;

namespace ClubPlaytime.Api.Services;

public interface IDiscordNotifier
{
    Task PlayerStartedAsync(Player player, string gameName, CancellationToken cancellationToken = default);

    Task PlayerStoppedAsync(Player player, string gameName, CancellationToken cancellationToken = default);

    /// <summary>Announces one or more achievements a player just unlocked (embed, with their avatar).</summary>
    Task AchievementUnlockedAsync(
        Player player,
        IReadOnlyList<(string Key, string Name, string Icon, DateTime? CrossedAt)> achievements,
        CancellationToken cancellationToken = default);

    /// <summary>Announces a streak milestone (embed, with their avatar).</summary>
    Task StreakMilestoneAsync(Player player, int streakDays, CancellationToken cancellationToken = default);
}
