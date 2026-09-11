namespace ClubPlaytime.Api.Models;

public sealed class JoinRequest
{
    public int Id { get; set; }

    public string RobloxUsername { get; set; } = string.Empty;

    public long RobloxUserId { get; set; }

    /// <summary>
    /// Website account that submitted this request (nullable for legacy
    /// anonymous requests submitted before accounts were linked to requests).
    /// </summary>
    public int? UserId { get; set; }

    public User? User { get; set; }

    public string DiscordUserId { get; set; } = string.Empty;

    public string Club { get; set; } = "PIH";

    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }
}
