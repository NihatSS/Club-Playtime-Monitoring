using System.ComponentModel.DataAnnotations;

namespace ClubPlaytime.Api.DTOs;

/// <summary>
/// Full profile for the authenticated user: account info, linked Roblox
/// tracker player (if any), Discord link state, and join-request status.
/// </summary>
public sealed class MyProfileResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? DiscordUserId { get; set; }

    /// <summary>Self-reported Roblox username shown until a tracker player is linked.</summary>
    public string? RobloxUsername { get; set; }

    /// <summary>Self-reported Roblox user ID shown until a tracker player is linked.</summary>
    public long? RobloxUserId { get; set; }

    /// <summary>Custom banner image URL for the profile hero (null when an uploaded image is used or the default gradient shows).</summary>
    public string? BannerUrl { get; set; }

    /// <summary>Cache-buster for the uploaded banner image (null = no uploaded image). Image lives at /api/profile/banner-image/{userId}.</summary>
    public string? BannerImageVersion { get; set; }

    public ProfilePlayerDto? Player { get; set; }

    public ProfileJoinRequestDto? JoinRequest { get; set; }

    /// <summary>Weekly leaderboard position (1-based) if the linked player qualifies; null otherwise.</summary>
    public int? WeeklyLeaderboardPosition { get; set; }

    public int? TotalLeaderboardPosition { get; set; }

    /// <summary>Streaks, achievement count and all-time rank derived from real tracker data.</summary>
    public PlayerProgressSummaryDto? Progress { get; set; }
}

public sealed class ProfilePlayerDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public long RobloxUserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Club { get; set; } = string.Empty;
    public long TodayPlaySeconds { get; set; }
    public long WeeklyPlaySeconds { get; set; }
    public long MonthlyPlaySeconds { get; set; }
    public long TotalPlaySeconds { get; set; }
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>Website presence: last time a linked website account was active on the site.</summary>
    public DateTime? LastSeenOnSite { get; set; }
}

public sealed class ProfileJoinRequestDto
{
    public int Id { get; set; }
    public string RobloxUsername { get; set; } = string.Empty;
    public long RobloxUserId { get; set; }
    public string? DiscordUserId { get; set; }
    public string Club { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
}

/// <summary>Set or clear the custom banner image on the caller's own profile.</summary>
public sealed class UpdateBannerRequest
{
    /// <summary>HTTPS image URL, or null/empty to reset to the default gradient.</summary>
    [MaxLength(700)]
    public string? BannerUrl { get; set; }
}

/// <summary>Link or unlink the current user's Discord account.</summary>
public sealed class UpdateDiscordRequest
{
    /// <summary>17-20 digit Discord snowflake, or null/empty to unlink.</summary>
    [MaxLength(20)]
    public string? DiscordUserId { get; set; }
}

/// <summary>
/// Game info a user fills in on their own profile: Roblox username, Roblox
/// user ID and Discord ID. Used to prefill their join request (Phase 10).
/// </summary>
public sealed class UpdateGameInfoRequest
{
    [MaxLength(100)]
    public string? RobloxUsername { get; set; }

    [Range(1, long.MaxValue)]
    public long? RobloxUserId { get; set; }

    [MaxLength(20)]
    public string? DiscordUserId { get; set; }
}

/// <summary>Admin: update a user's profile information.</summary>
public sealed class AdminUpdateUserRequest
{
    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";

    public string? DiscordUserId { get; set; }

    public int? PlayerId { get; set; }
}

/// <summary>Admin: detailed user info for the management view.</summary>
public sealed class AdminUserDetailDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? DiscordUserId { get; set; }
    public int? PlayerId { get; set; }
    public string? PlayerUsername { get; set; }
    public long? PlayerRobloxUserId { get; set; }
}
