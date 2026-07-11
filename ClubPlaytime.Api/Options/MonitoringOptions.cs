namespace ClubPlaytime.Api.Options;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int CheckIntervalSeconds { get; set; } = 60;

    public string TargetGameName { get; set; } = "Racket Rivals";

    public string RobloxBaseUrl { get; set; } = "https://www.roblox.com";

    public int RequestTimeoutSeconds { get; set; } = 20;

    public bool EnableDiscordNotifications { get; set; }

    public string? DiscordWebhookUrl { get; set; }

    /// <summary>
    /// Optional .ROBLOSECURITY cookie for authenticated Presence API lookups.
    /// When set, the API can return the actual game name (lastLocation) for in-game users.
    /// Without it, in-game status is detected but the game name may be empty.
    /// </summary>
    public string? RobloxSecurityCookie { get; set; }

    public string AvatarThumbnailUrl { get; set; } =
        "https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={0}&size=150x150&format=Png&isCircular=false";
}
