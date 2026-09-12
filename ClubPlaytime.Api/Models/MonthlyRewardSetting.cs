namespace ClubPlaytime.Api.Models;

/// <summary>
/// Singleton setting describing the reward given to the player with the most
/// playtime in the current calendar month.
/// </summary>
public sealed class MonthlyRewardSetting
{
    /// <summary>Singleton row id — always 1.</summary>
    public int Id { get; set; }

    /// <summary>Reward description shown on the rewards page (e.g. "10 USD Roblox gift card").</summary>
    public string Prize { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Admin who last updated the prize.</summary>
    public string? UpdatedBy { get; set; }
}
