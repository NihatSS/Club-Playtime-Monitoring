namespace ClubPlaytime.Api.Models;

/// <summary>Format a tournament can be played in.</summary>
public enum TournamentFormat
{
    SingleElimination = 1
}

/// <summary>Team mode: solo (1v1) or duo (2v2, random or predefined teams).</summary>
public enum TournamentTeamMode
{
    Solo = 1,          // 1v1
    DuoRandom = 2,     // 2v2 random teams
    DuoPredefined = 3  // 2v2 predefined teams
}

/// <summary>Lifecycle of a tournament. Stored as a string in the database.</summary>
public static class TournamentStatus
{
    public const string Draft = "DRAFT";
    public const string RegistrationOpen = "REGISTRATION_OPEN";
    public const string RegistrationClosed = "REGISTRATION_CLOSED";
    public const string Upcoming = "UPCOMING";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Draft, RegistrationOpen, RegistrationClosed, Upcoming, InProgress, Completed, Cancelled
    };
}

/// <summary>A tournament entity with schedule, format, prizes, and lifecycle status.</summary>
public sealed class Tournament
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>Free-form rules, one per line (rendered as a bullet list).</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(4000)]
    public string? RulesText { get; set; }

    /// <summary>Game format (e.g. Single Elimination).</summary>
    public TournamentFormat Format { get; set; } = TournamentFormat.SingleElimination;

    /// <summary>Team mode (1v1, 2v2 random, 2v2 predefined).</summary>
    public TournamentTeamMode TeamMode { get; set; } = TournamentTeamMode.Solo;

    public DateTime RegistrationStartsAt { get; set; }

    public DateTime RegistrationDeadline { get; set; }

    /// <summary>Separate from creation/announcement time — the tournament does not start on creation.</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>Maximum participants (1v1: players, 2v2: players). 0 = unlimited.</summary>
    public int MaxParticipants { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(500)]
    public string? PrizeInfo { get; set; }

    /// <summary>One of TournamentStatus constants. Defaults to DRAFT — never started automatically.</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(30)]
    public string Status { get; set; } = TournamentStatus.Draft;

    /// <summary>The user who created/announced this tournament.</summary>
    public int? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Winners recorded when the final completes (denormalized for history views).</summary>
    public int? FirstPlaceParticipantId { get; set; }
    public int? SecondPlaceParticipantId { get; set; }
    public int? ThirdPlaceParticipantId { get; set; }

    /// <summary>Placement names snapshotted at completion (works for both 1v1 and 2v2 team names).</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(120)]
    public string? FirstPlaceName { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(120)]
    public string? SecondPlaceName { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(120)]
    public string? ThirdPlaceName { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentPrize> Prizes { get; set; } = new List<TournamentPrize>();
}

/// <summary>A registered participant (player-based so admin can add any tracker player).</summary>
public sealed class TournamentParticipant
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>The tracker player participating. Linked website user, if any, is derived.</summary>
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>The website account that registered (null if an admin registered a player directly).</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>True when the participant was removed/disqualified by an admin.</summary>
    public bool IsDisqualified { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string? DisqualifiedReason { get; set; }

    /// <summary>Team membership when the tournament is 2v2. Null in solo tournaments.</summary>
    public int? TeamId { get; set; }
    public TournamentTeam? Team { get; set; }
}

/// <summary>A team of up to 2 players for 2v2 tournaments.</summary>
public sealed class TournamentTeam
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>True when generated randomly rather than predefined by an admin.</summary>
    public bool IsRandomTeam { get; set; }

    public int Seed { get; set; }

    public ICollection<TournamentParticipant> Members { get; set; } = new List<TournamentParticipant>();
}

/// <summary>A single elimination match stored persistently (bracket survives refreshes).</summary>
public sealed class TournamentMatch
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Round number: 1 = Quarter Finals, 2 = Semi Finals, 3 = Final (larger for bigger brackets).</summary>
    public int Round { get; set; }

    /// <summary>Position of the match within its round, used for slotting winners into the next round.</summary>
    public int Slot { get; set; }

    /// <summary>Participant 1 (1v1 tournaments). Null in 2v2 or until the slot is filled.</summary>
    public int? Participant1Id { get; set; }
    public TournamentParticipant? Participant1 { get; set; }

    public int? Participant2Id { get; set; }
    public TournamentParticipant? Participant2 { get; set; }

    /// <summary>Team 1 (2v2 tournaments). Null in 1v1 or until the slot is filled.</summary>
    public int? Team1Id { get; set; }
    public TournamentTeam? Team1 { get; set; }

    public int? Team2Id { get; set; }
    public TournamentTeam? Team2 { get; set; }

    /// <summary>Winner participant (1v1). Null while the match is undecided.</summary>
    public int? WinnerId { get; set; }
    public TournamentParticipant? Winner { get; set; }

    /// <summary>Winning team (2v2). Null while the match is undecided.</summary>
    public int? WinnerTeamId { get; set; }
    public TournamentTeam? WinnerTeam { get; set; }

    /// <summary>One of TournamentMatchStatus constants.</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public string Status { get; set; } = TournamentMatchStatus.Pending;

    public DateTime? PlayedAt { get; set; }

    /// <summary>The next match this winner advances into (null for the final).</summary>
    public int? NextMatchId { get; set; }
    public TournamentMatch? NextMatch { get; set; }

    /// <summary>Which slot in the next match the winner fills (1 or 2).</summary>
    public int? NextMatchSlot { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string? Note { get; set; }
}

public static class TournamentMatchStatus
{
    public const string Pending = "PENDING";
    public const string Ready = "READY";
    public const string Completed = "COMPLETED";

    public static readonly IReadOnlyList<string> All = new[] { Pending, Ready, Completed };
}

/// <summary>Configurable prize per placement (optional, never hardcoded).</summary>
public sealed class TournamentPrize
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Placement the prize is awarded for (1 = first, 2 = second, 3 = third).</summary>
    public int Placement { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(300)]
    public string Description { get; set; } = string.Empty;
}
