namespace ClubPlaytime.Api.Models;

public sealed class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User"; // "User" or "Admin"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The tracker player claimed by this account (nullable until the user
    /// claims an existing player or is approved via a join request).
    /// A player can only be linked to one website account.
    /// </summary>
    public int? PlayerId { get; set; }

    public Player? Player { get; set; }

    /// <summary>Discord user ID if the user has linked Discord (optional).</summary>
    public string? DiscordUserId { get; set; }
}