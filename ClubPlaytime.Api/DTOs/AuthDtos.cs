using System.ComponentModel.DataAnnotations;

namespace ClubPlaytime.Api.DTOs;

public sealed class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public sealed class CreateUserRequest
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional claim token obtained after Roblox ownership verification.</summary>
    public string? ClaimToken { get; set; }
}

public sealed class VerifyStartRequest
{
    [Required]
    public long RobloxUserId { get; set; }
}

public sealed class VerifyStartResponse
{
    public int VerificationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RobloxUsername { get; set; } = string.Empty;
    public long RobloxUserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Club { get; set; } = string.Empty;
}

public sealed class VerifyCheckRequest
{
    [Required]
    public int VerificationId { get; set; }

    [Required, MaxLength(32)]
    public string Code { get; set; } = string.Empty;
}

public sealed class VerifyCheckResponse
{
    public bool Verified { get; set; }
    public string? ClaimToken { get; set; }
    public string? Message { get; set; }
    public ClaimedPlayerDto? Player { get; set; }
}

public sealed class ClaimedPlayerDto
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public long RobloxUserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Club { get; set; } = string.Empty;
    public long TotalPlaySeconds { get; set; }
}

public sealed class PlayerSearchResultDto
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public long RobloxUserId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Club { get; set; } = string.Empty;

    /// <summary>True if this player is already claimed by a website account.</summary>
    public bool IsClaimed { get; set; }
}

public sealed class MeResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? PlayerId { get; set; }
    public string? DiscordUserId { get; set; }
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ImportPlaytimeRequest
{
    public List<ImportDailyPlaytimeDto>? DailyPlaytime { get; set; }
}

public sealed class ImportDailyPlaytimeDto{
    public long RobloxUserId { get; set; }
    public string Date { get; set; } = string.Empty;
    public long PlaySeconds { get; set; }
}
