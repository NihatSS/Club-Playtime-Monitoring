using System.ComponentModel.DataAnnotations;

namespace ClubPlaytime.Api.DTOs;

public sealed class SubmitJoinRequest
{
    [Required, MaxLength(100)]
    public string RobloxUsername { get; set; } = string.Empty;

    [Required]
    public long RobloxUserId { get; set; }

    [Required, MaxLength(100)]
    public string DiscordUserId { get; set; } = string.Empty;

    [Required]
    public string Club { get; set; } = "PIH";

    [MaxLength(500)]
    public string? Note { get; set; }
}

public sealed class JoinRequestDto
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

public sealed class ReviewJoinRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // Approved or Rejected
}
