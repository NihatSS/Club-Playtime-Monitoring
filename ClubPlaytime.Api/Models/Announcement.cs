namespace ClubPlaytime.Api.Models;

/// <summary>
/// Admin-posted announcement shown in the navbar notification feed
/// (tournament news, giveaways, community messages, ...).
/// </summary>
public sealed class Announcement
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    /// <summary>
    /// Optional in-app hash route the notification navigates to
    /// (e.g. "tournaments/3"). Null means the announcement is informational.
    /// </summary>
    public string? LinkUrl { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
