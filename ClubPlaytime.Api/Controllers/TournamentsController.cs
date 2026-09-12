using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TournamentsController(
    ClubPlaytimeDbContext dbContext,
    TournamentService tournamentService,
    ILogger<TournamentsController> logger) : ControllerBase
{
    private const string AdminRole = "Admin";

    private bool IsAdmin => User.IsInRole(AdminRole);

    // ─── Queries ─────────────────────────────────────────────────

    /// <summary>All tournaments visible to everyone (history included).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TournamentListItemDto>>> GetTournaments()
    {
        var tournaments = await dbContext.Tournaments
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var tournamentIds = tournaments.Select(t => t.Id).ToList();

        var participantCounts = await dbContext.TournamentParticipants
            .Where(p => tournamentIds.Contains(p.TournamentId) && !p.IsDisqualified)
            .GroupBy(p => p.TournamentId)
            .Select(g => new { TournamentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TournamentId, x => x.Count);

        var winnerIds = tournaments
            .Where(t => t.FirstPlaceParticipantId != null)
            .Select(t => t.FirstPlaceParticipantId!.Value)
            .ToList();

        var winnerNames = await dbContext.TournamentParticipants
            .Where(p => winnerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Player.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Username);

        var result = tournaments.Select(t => new TournamentListItemDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            TeamMode = t.TeamMode.ToString(),
            Status = t.Status,
            RegistrationStartsAt = t.RegistrationStartsAt,
            RegistrationDeadline = t.RegistrationDeadline,
            StartsAt = t.StartsAt,
            MaxParticipants = t.MaxParticipants,
            ParticipantCount = participantCounts.GetValueOrDefault(t.Id),
            PrizeInfo = t.PrizeInfo,
            RulesText = t.RulesText,
            CreatedAt = t.CreatedAt,
            CompletedAt = t.CompletedAt,
            WinnerName = t.FirstPlaceName
                ?? (t.FirstPlaceParticipantId != null && winnerNames.TryGetValue(t.FirstPlaceParticipantId.Value, out var wn)
                    ? wn
                    : null)
        }).ToList();

        return Ok(result);
    }

    /// <summary>Detail including participants, teams, persistent bracket and prizes.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TournamentDetailDto>> GetTournament(int id)
    {
        var tournament = await dbContext.Tournaments
            .Include(t => t.Participants).ThenInclude(p => p.Player)
            .Include(t => t.Teams).ThenInclude(tm => tm.Members).ThenInclude(p => p.Player)
            .Include(t => t.Matches)
            .Include(t => t.Prizes)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        var username = User.Identity?.Name;
        int? currentPlayerId = null;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user?.PlayerId != null)
            {
                currentPlayerId = user.PlayerId;
            }
        }

        var detail = MapDetail(tournament, currentPlayerId);
        return Ok(detail);
    }

    // ─── Admin CRUD ──────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult<TournamentDetailDto>> CreateTournament(CreateTournamentRequest request)
    {
        var username = User.Identity?.Name;
        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

        var teamMode = ParseTeamMode(request.TeamMode);

        if (request.RegistrationDeadline <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Registration deadline must be in the future." });
        }

        if (request.StartsAt != null && request.StartsAt < request.RegistrationDeadline)
        {
            return BadRequest(new { message = "Tournament start must be after the registration deadline." });
        }

        var registrationStart = request.RegistrationStartsAt ?? DateTime.UtcNow;

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            RulesText = request.RulesText?.Trim(),
            TeamMode = teamMode,
            RegistrationStartsAt = registrationStart,
            RegistrationDeadline = request.RegistrationDeadline ?? registrationStart.AddDays(7),
            StartsAt = request.StartsAt ?? registrationStart.AddDays(8),
            MaxParticipants = request.MaxParticipants,
            PrizeInfo = request.PrizeInfo?.Trim(),
            Status = TournamentStatus.Draft,
            CreatedByUserId = adminUser?.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        if (request.Prizes is { Count: > 0 })
        {
            await ReplacePrizesAsync(tournament, request.Prizes);
        }

        return CreatedAtAction(nameof(GetTournament), new { id = tournament.Id },
            await BuildFreshDetailAsync(tournament.Id));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> UpdateTournament(int id, UpdateTournamentRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        if (tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Cancelled)
        {
            return BadRequest(new { message = "Completed or cancelled tournaments cannot be edited." });
        }

        if (request.RegistrationDeadline <= DateTime.UtcNow
            && tournament.Status == TournamentStatus.Draft)
        {
            return BadRequest(new { message = "Registration deadline must be in the future." });
        }

        if (request.StartsAt != null && request.RegistrationDeadline != null
            && request.StartsAt < request.RegistrationDeadline)
        {
            return BadRequest(new { message = "Tournament start must be after the registration deadline." });
        }

        tournament.Name = request.Name.Trim();
        tournament.Description = request.Description?.Trim();
        tournament.RulesText = request.RulesText?.Trim();
        tournament.RegistrationStartsAt = request.RegistrationStartsAt ?? tournament.RegistrationStartsAt;
        tournament.RegistrationDeadline = request.RegistrationDeadline ?? tournament.RegistrationDeadline;
        tournament.StartsAt = request.StartsAt ?? tournament.StartsAt;
        tournament.MaxParticipants = request.MaxParticipants;
        tournament.PrizeInfo = request.PrizeInfo?.Trim();
        tournament.UpdatedAt = DateTime.UtcNow;

        if (request.Prizes != null)
        {
            await ReplacePrizesAsync(tournament, request.Prizes);
        }

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> DeleteTournament(int id, [FromServices] TournamentService tournamentService)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        // Sever the self-referencing match links before removing the tournament,
        // otherwise the NextMatchId restrict FK blocks the cascade.
        await tournamentService.DeleteBracketForceAsync(tournament);

        dbContext.Tournaments.Remove(tournament);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Explicit status transitions. Nothing auto-starts a tournament.</summary>
    [HttpPost("{id:int}/status")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> SetStatus(int id, SetTournamentStatusRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        var requested = request.Status.Trim().ToUpperInvariant();
        if (!TournamentStatus.All.Contains(requested))
        {
            return BadRequest(new { message = "Unknown tournament status." });
        }

        // Some transitions are gated: entering IN_PROGRESS happens via bracket
        // generation (or this endpoint when a bracket already exists).
        if (requested == TournamentStatus.InProgress && tournament.Status != TournamentStatus.InProgress)
        {
            var hasBracket = await dbContext.TournamentMatches.AnyAsync(m => m.TournamentId == id);
            if (!hasBracket)
            {
                return BadRequest(new { message = "Generate the bracket before starting the tournament." });
            }
        }

        tournament.Status = requested;
        tournament.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Cancel(int id)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        if (tournament.Status == TournamentStatus.Completed)
        {
            return BadRequest(new { message = "A completed tournament cannot be cancelled." });
        }

        tournament.Status = TournamentStatus.Cancelled;
        tournament.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    // ─── Registration ────────────────────────────────────────────

    /// <summary>Self-registration for authenticated users (uses their linked tracker player).</summary>
    [HttpPost("{id:int}/register")]
    [Authorize]
    public async Task<IActionResult> RegisterSelf(int id, RegisterSelfRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        var username = User.Identity?.Name;
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        // Resolve which tracker player this account may register as.
        var playerId = request.PlayerId != 0 ? request.PlayerId : user.PlayerId;
        if (playerId == null)
        {
            return BadRequest(new { message = "Your account is not linked to a tracker player yet." });
        }

        // Non-admins can only register themselves (their own linked player).
        if (!IsAdmin && playerId != user.PlayerId)
        {
            return Forbid();
        }

        try
        {
            await tournamentService.RegisterPlayerAsync(tournament, playerId.Value, user.Id);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new { message = "Registered successfully." });
    }

    /// <summary>Admin can register any tracker player directly.</summary>
    [HttpPost("{id:int}/participants")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> AdminRegisterPlayer(int id, AdminRegisterPlayerRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.AdminRegisterPlayerAsync(tournament, request.PlayerId);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new { message = "Player added." });
    }

    [HttpDelete("{id:int}/participants/{participantId:int}")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> RemoveParticipant(int id, int participantId)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.RemoveParticipantAsync(tournament, participantId);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/participants/{participantId:int}/disqualify")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> DisqualifyParticipant(
        int id, int participantId, DisqualifyParticipantRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.DisqualifyParticipantAsync(tournament, participantId, request.Reason);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    /// <summary>Un-disqualify: clears the flag so the participant can be re-slotted by the admin.</summary>
    [HttpPost("{id:int}/participants/{participantId:int}/requalify")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> RequalifyParticipant(int id, int participantId)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        var participant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.TournamentId == id);
        if (participant is null)
        {
            return NotFound(new { message = "Participant not found." });
        }

        participant.IsDisqualified = false;
        participant.DisqualifiedReason = null;
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    // ─── Teams ───────────────────────────────────────────────────

    [HttpPost("{id:int}/teams")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> CreateTeam(int id, CreateTeamRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.CreateTeamAsync(tournament, request.Name.Trim());
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new { message = "Team created." });
    }

    [HttpDelete("{id:int}/teams/{teamId:int}")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> DeleteTeam(int id, int teamId)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.DeleteTeamAsync(tournament, teamId);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/teams/{teamId:int}/members")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> AssignTeamMember(int id, int teamId, AssignTeamMemberRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.AssignParticipantToTeamAsync(tournament, request.ParticipantId, teamId);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new { message = "Player assigned." });
    }

    [HttpDelete("{id:int}/teams/{teamId:int}/members/{participantId:int}")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> RemoveTeamMember(int id, int teamId, int participantId)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.RemoveParticipantFromTeamAsync(tournament, participantId);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/teams/{teamId:int}/replace-player")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> ReplaceTeamPlayer(int id, int teamId, ReplaceTeamPlayerRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.ReplaceTeamPlayerAsync(
                tournament, teamId, request.OldParticipantId, request.NewParticipantId);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new { message = "Player replaced." });
    }

    /// <summary>Randomly pairs all unassigned participants into teams of 2.</summary>
    [HttpPost("{id:int}/teams/random")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> CreateRandomTeams(int id)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            var teams = await tournamentService.CreateRandomTeamsAsync(tournament);
            return Ok(new { message = $"Created {teams.Count} random teams." });
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Bracket & matches ───────────────────────────────────────

    [HttpPost("{id:int}/bracket")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> GenerateBracket(int id)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.GenerateBracketAsync(tournament);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bracket generation failed for tournament {TournamentId}", id);
            return StatusCode(500, new { message = "Bracket generation failed. Please try again." });
        }

        return Ok(new { message = "Bracket generated. Tournament is now in progress." });
    }

    [HttpDelete("{id:int}/bracket")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> DeleteBracket(int id)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.DeleteBracketAsync(tournament);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    /// <summary>
    /// Records or changes a match winner. When overriding a completed match that
    /// already influenced later rounds, the client must send ConfirmCascade=true.
    /// </summary>
    [HttpPost("{id:int}/matches/{matchId:int}/winner")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> SetMatchWinner(int id, int matchId, SetMatchWinnerRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        var match = await dbContext.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == id);
        if (match is null)
        {
            return NotFound(new { message = "Match not found." });
        }

        // Cascade warning: changing a completed match that already fed the next round.
        var affectsLaterRounds = match.Status == TournamentMatchStatus.Completed
                                 && match.WinnerId != null
                                 && match.NextMatchId != null
                                 && request.WinnerParticipantId != match.WinnerId;

        if (affectsLaterRounds && !request.ConfirmCascade)
        {
            return Conflict(new
            {
                message = "This match was already recorded and its winner advanced. Changing it will update the next round. Confirm to continue.",
                requiresConfirmation = true
            });
        }

        try
        {
            await tournamentService.SetMatchWinnerAsync(tournament, matchId, request.WinnerParticipantId, request.Note);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        // Completing the final marks the whole tournament complete (1st/2nd/3rd).
        if (match.NextMatchId == null
            && request.WinnerParticipantId != null
            && tournament.Status == TournamentStatus.InProgress)
        {
            try
            {
                await tournamentService.TryCompleteTournamentAsync(tournament);
            }
            catch (TournamentRuleException)
            {
                // Final not fully decided yet — nothing to do.
            }
        }

        return Ok(new { message = "Match updated." });
    }

    [HttpPost("{id:int}/matches/{matchId:int}/participants")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> SetMatchParticipants(
        int id, int matchId, SetMatchParticipantsRequest request)
    {
        var tournament = await dbContext.Tournaments.FindAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = "Tournament not found." });
        }

        try
        {
            await tournamentService.SetMatchParticipantsAsync(
                tournament, matchId, request.Participant1Id, request.Participant2Id);
        }
        catch (TournamentRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new { message = "Match participants updated." });
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private static TournamentTeamMode ParseTeamMode(string value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "solo" or "1v1" => TournamentTeamMode.Solo,
            "duorandom" or "2v2random" or "random" => TournamentTeamMode.DuoRandom,
            "duopredefined" or "2v2predefined" or "predefined" => TournamentTeamMode.DuoPredefined,
            _ => TournamentTeamMode.Solo
        };
    }

    private async Task ReplacePrizesAsync(Tournament tournament, List<PrizeInputDto> prizes)
    {
        var existing = await dbContext.TournamentPrizes
            .Where(p => p.TournamentId == tournament.Id)
            .ToListAsync();
        dbContext.TournamentPrizes.RemoveRange(existing);

        foreach (var prize in prizes.Where(p => !string.IsNullOrWhiteSpace(p.Description)))
        {
            dbContext.TournamentPrizes.Add(new TournamentPrize
            {
                TournamentId = tournament.Id,
                Placement = prize.Placement,
                Description = prize.Description.Trim()
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<TournamentDetailDto> BuildFreshDetailAsync(int tournamentId)
    {
        var tournament = await dbContext.Tournaments
            .Include(t => t.Participants).ThenInclude(p => p.Player)
            .Include(t => t.Teams).ThenInclude(tm => tm.Members).ThenInclude(p => p.Player)
            .Include(t => t.Matches)
            .Include(t => t.Prizes)
            .FirstAsync(t => t.Id == tournamentId);

        return MapDetail(tournament, currentPlayerId: null);
    }

    private static TournamentDetailDto MapDetail(Tournament tournament, int? currentPlayerId)
    {
        var participantsById = tournament.Participants.ToDictionary(p => p.Id);
        var teamsById = tournament.Teams.ToDictionary(t => t.Id);
        var isTeamBracket = tournament.TeamMode != TournamentTeamMode.Solo;

        // Resolve the display entrant for a match slot: in 1v1 slots hold
        // participant ids, in 2v2 they hold team ids.
        (string? Name, string? Avatar) EntrantOf(int? participantId, int? teamId)
        {
            if (isTeamBracket)
            {
                if (teamId != null && teamsById.TryGetValue(teamId.Value, out var team))
                {
                    return (team.Name, null);
                }
                return (null, null);
            }

            if (participantId != null && participantsById.TryGetValue(participantId.Value, out var p))
            {
                return (p.Player?.Username ?? "Unknown", p.Player?.AvatarUrl);
            }
            return (null, null);
        }

        var detail = new TournamentDetailDto
        {
            Id = tournament.Id,
            Name = tournament.Name,
            Description = tournament.Description,
            TeamMode = tournament.TeamMode.ToString(),
            Status = tournament.Status,
            RegistrationStartsAt = tournament.RegistrationStartsAt,
            RegistrationDeadline = tournament.RegistrationDeadline,
            StartsAt = tournament.StartsAt,
            MaxParticipants = tournament.MaxParticipants,
            PrizeInfo = tournament.PrizeInfo,
            RulesText = tournament.RulesText,
            CreatedAt = tournament.CreatedAt,
            CompletedAt = tournament.CompletedAt,
            IsRegistered = currentPlayerId != null && tournament.Participants.Any(p => p.UserId == currentPlayerId
                                                                                    || p.PlayerId == currentPlayerId)
        };

        string? WinnerNameOf(TournamentMatch m) =>
            isTeamBracket
                ? (m.WinnerTeamId != null && teamsById.TryGetValue(m.WinnerTeamId.Value, out var wt) ? wt.Name : null)
                : (m.WinnerId != null && participantsById.TryGetValue(m.WinnerId.Value, out var wp)
                    ? wp.Player?.Username ?? "Unknown"
                    : null);

        detail.Participants = tournament.Participants
            .OrderBy(p => p.RegisteredAt)
            .Select(p => new TournamentParticipantDto
            {
                Id = p.Id,
                PlayerId = p.PlayerId,
                Username = p.Player?.Username ?? "Unknown",
                AvatarUrl = p.Player?.AvatarUrl,
                TeamId = p.TeamId,
                RegisteredAt = p.RegisteredAt,
                IsDisqualified = p.IsDisqualified,
                DisqualifiedReason = p.DisqualifiedReason,
                UserId = p.UserId
            }).ToList();

        detail.Teams = tournament.Teams
            .OrderBy(t => t.Id)
            .Select(t => new TournamentTeamDto
            {
                Id = t.Id,
                Name = t.Name,
                IsRandomTeam = t.IsRandomTeam,
                Members = t.Members.Select(m => new TournamentParticipantDto
                {
                    Id = m.Id,
                    PlayerId = m.PlayerId,
                    Username = m.Player?.Username ?? "Unknown",
                    AvatarUrl = m.Player?.AvatarUrl,
                    TeamId = m.TeamId
                }).ToList()
            }).ToList();

        detail.Matches = tournament.Matches
            .OrderBy(m => m.Round).ThenBy(m => m.Slot)
            .Select(m =>
            {
                var (p1Name, p1Avatar) = EntrantOf(m.Participant1Id, m.Team1Id);
                var (p2Name, p2Avatar) = EntrantOf(m.Participant2Id, m.Team2Id);
                var winnerName = WinnerNameOf(m);

                // In 2v2 the DTO slot ids carry TEAM ids so the frontend's
                // "set winner" button posts the correct value for both modes.
                var dtoSlot1 = isTeamBracket ? m.Team1Id : m.Participant1Id;
                var dtoSlot2 = isTeamBracket ? m.Team2Id : m.Participant2Id;
                var winnerSlot = isTeamBracket ? m.WinnerTeamId : m.WinnerId;

                return new TournamentMatchDto
                {
                    Id = m.Id,
                    Round = m.Round,
                    Slot = m.Slot,
                    Participant1Id = dtoSlot1,
                    Participant2Id = dtoSlot2,
                    Participant1Name = p1Name,
                    Participant2Name = p2Name,
                    Participant1Avatar = p1Avatar,
                    Participant2Avatar = p2Avatar,
                    WinnerId = winnerSlot != null && (winnerSlot == dtoSlot1 || winnerSlot == dtoSlot2)
                        ? winnerSlot
                        : null,
                    WinnerName = winnerName,
                    Status = m.Status,
                    PlayedAt = m.PlayedAt,
                    Note = m.Note,
                    NextMatchId = m.NextMatchId,
                    NextMatchSlot = m.NextMatchSlot
                };
            }).ToList();

        detail.Prizes = tournament.Prizes
            .OrderBy(p => p.Placement)
            .Select(p => new TournamentPrizeDto
            {
                Placement = p.Placement,
                Description = p.Description
            }).ToList();

        if (tournament.Status == TournamentStatus.Completed)
        {
            detail.Results = new TournamentResultsDto
            {
                FirstPlace = tournament.FirstPlaceName
                    ?? (tournament.FirstPlaceParticipantId != null && participantsById.TryGetValue(tournament.FirstPlaceParticipantId.Value, out var fp)
                        ? fp.Player?.Username
                        : null),
                SecondPlace = tournament.SecondPlaceName
                    ?? (tournament.SecondPlaceParticipantId != null && participantsById.TryGetValue(tournament.SecondPlaceParticipantId.Value, out var sp)
                        ? sp.Player?.Username
                        : null),
                ThirdPlace = tournament.ThirdPlaceName
                    ?? (tournament.ThirdPlaceParticipantId != null && participantsById.TryGetValue(tournament.ThirdPlaceParticipantId.Value, out var tp)
                        ? tp.Player?.Username
                        : null),
                CompletedAt = tournament.CompletedAt
            };
        }

        return detail;
    }
}
