namespace ClubPlaytime.Api.Options;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int CheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// How long the monitor reuses its in-memory player roster before re-reading
    /// the Players table. Re-reading it every cycle would itself keep a serverless
    /// database awake 24/7, so this is deliberately measured in minutes; player
    /// adds/removes and the admin "check now" button refresh it immediately.
    /// </summary>
    public int RosterCacheSeconds { get; set; } = 600;

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
