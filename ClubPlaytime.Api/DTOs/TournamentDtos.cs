using System.ComponentModel.DataAnnotations;

namespace ClubPlaytime.Api.DTOs;

// ─── Tournament CRUD (admin) ─────────────────────────────────

public sealed class CreateTournamentRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>Free-form rules, one per line.</summary>
    [MaxLength(4000)]
    public string? RulesText { get; set; }

    /// <summary>"Solo" (1v1), "DuoRandom" (2v2 random) or "DuoPredefined" (2v2 predefined).</summary>
    [Required]
    public string TeamMode { get; set; } = "Solo";

    public DateTime? RegistrationStartsAt { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    /// <summary>Tournament start — independent of creation time. Tournaments never auto-start.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Max participants (players). 0 = unlimited.</summary>
    [Range(0, 1024)]
    public int MaxParticipants { get; set; }

    [MaxLength(500)]
    public string? PrizeInfo { get; set; }

    public List<PrizeInputDto>? Prizes { get; set; }
}

public sealed class UpdateTournamentRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>Free-form rules, one per line.</summary>
    [MaxLength(4000)]
    public string? RulesText { get; set; }

    public DateTime? RegistrationStartsAt { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    public DateTime? StartsAt { get; set; }

    [Range(0, 1024)]
    public int MaxParticipants { get; set; }

    [MaxLength(500)]
    public string? PrizeInfo { get; set; }

    public List<PrizeInputDto>? Prizes { get; set; }
}

public sealed class PrizeInputDto
{
    /// <summary>1 = first place, 2 = second, 3 = third.</summary>
    [Range(1, 3)]
    public int Placement { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;
}

// ─── Registration ────────────────────────────────────────────

public sealed class RegisterSelfRequest
{
    /// <summary>The tracker player the user wants to register as (usually their linked player).</summary>
    [Required]
    public int PlayerId { get; set; }
}

public sealed class AdminRegisterPlayerRequest
{
    [Required]
    public int PlayerId { get; set; }
}

// ─── Teams ───────────────────────────────────────────────────

public sealed class CreateTeamRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public sealed class AssignTeamMemberRequest
{
    [Required]
    public int ParticipantId { get; set; }
}

public sealed class ReplaceTeamPlayerRequest
{
    [Required]
    public int OldParticipantId { get; set; }

    [Required]
    public int NewParticipantId { get; set; }
}

// ─── Matches ─────────────────────────────────────────────────

public sealed class SetMatchWinnerRequest
{
    /// <summary>Participant to record as winner, or null to reset the match.</summary>
    public int? WinnerParticipantId { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }

    /// <summary>Set when the admin overrides a completed match that already fed later rounds.</summary>
    public bool ConfirmCascade { get; set; }
}

public sealed class SetMatchParticipantsRequest
{
    public int? Participant1Id { get; set; }

    public int? Participant2Id { get; set; }
}

public sealed class DisqualifyParticipantRequest
{
    [MaxLength(200)]
    public string? Reason { get; set; }
}

// ─── Status transitions ──────────────────────────────────────

public sealed class SetTournamentStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

// ─── Read models ─────────────────────────────────────────────

public sealed class TournamentListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TeamMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationStartsAt { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public DateTime StartsAt { get; set; }
    public int MaxParticipants { get; set; }
    public int ParticipantCount { get; set; }
    public string? PrizeInfo { get; set; }
    public string? RulesText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? WinnerName { get; set; }
}

public sealed class TournamentDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TeamMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationStartsAt { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public DateTime StartsAt { get; set; }
    public int MaxParticipants { get; set; }
    public string? PrizeInfo { get; set; }
    public string? RulesText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsRegistered { get; set; }

    public List<TournamentParticipantDto> Participants { get; set; } = new();
    public List<TournamentTeamDto> Teams { get; set; } = new();
    public List<TournamentMatchDto> Matches { get; set; } = new();
    public List<TournamentPrizeDto> Prizes { get; set; } = new();

    public TournamentResultsDto? Results { get; set; }
}

public sealed class TournamentParticipantDto
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int? TeamId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public bool IsDisqualified { get; set; }
    public string? DisqualifiedReason { get; set; }
    public int? UserId { get; set; }
}

public sealed class TournamentTeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRandomTeam { get; set; }
    public List<TournamentParticipantDto> Members { get; set; } = new();
}

public sealed class TournamentMatchDto
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int Slot { get; set; }
    public int? Participant1Id { get; set; }
    public int? Participant2Id { get; set; }
    public string? Participant1Name { get; set; }
    public string? Participant2Name { get; set; }
    public string? Participant1Avatar { get; set; }
    public string? Participant2Avatar { get; set; }
    /// <summary>Id of the winning slot (participant id in 1v1, team id in 2v2) for highlighting.</summary>
    public int? WinnerId { get; set; }
    public string? WinnerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PlayedAt { get; set; }
    public string? Note { get; set; }
    public int? NextMatchId { get; set; }
    public int? NextMatchSlot { get; set; }
}

public sealed class TournamentPrizeDto
{
    public int Placement { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class TournamentResultsDto
{
    public string? FirstPlace { get; set; }
    public string? SecondPlace { get; set; }
    public string? ThirdPlace { get; set; }
    public DateTime? CompletedAt { get; set; }
}
