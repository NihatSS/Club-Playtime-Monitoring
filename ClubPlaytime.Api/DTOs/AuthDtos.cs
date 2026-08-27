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
    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
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
