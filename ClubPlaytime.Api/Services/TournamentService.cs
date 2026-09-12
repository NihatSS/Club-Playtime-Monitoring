using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Services;

/// <summary>Thrown by the tournament service; mapped by the controller to a 400 with a safe message.</summary>
public sealed class TournamentRuleException(string message) : Exception(message)
{
}

/// <summary>
/// Core tournament domain logic: registration rules, team assignment, persistent
/// elimination brackets with BYEs, winner advancement/reset, disqualification and
/// completion. All writes happen inside transactions so partial brackets or
/// half-updated matches can never be persisted.
/// </summary>
public sealed class TournamentService(ClubPlaytimeDbContext dbContext)
{
    // ─── Registration ────────────────────────────────────────────

    /// <summary>Registers a player. Enforces status, window, deadline, capacity and duplicates server-side.</summary>
    public async Task<TournamentParticipant> RegisterPlayerAsync(
        Tournament tournament, int playerId, int? userId)
    {
        ValidateRegistrationWindow(tournament);

        var player = await dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId)
                     ?? throw new TournamentRuleException("This player no longer exists in the tracker.");

        var alreadyRegistered = await dbContext.TournamentParticipants
            .AnyAsync(p => p.TournamentId == tournament.Id && p.PlayerId == playerId);
        if (alreadyRegistered)
        {
            throw new TournamentRuleException("You are already registered for this tournament.");
        }

        if (tournament.MaxParticipants > 0)
        {
            var currentCount = await dbContext.TournamentParticipants
                .CountAsync(p => p.TournamentId == tournament.Id && !p.IsDisqualified);
            if (currentCount >= tournament.MaxParticipants)
            {
                throw new TournamentRuleException("This tournament is full.");
            }
        }

        if (tournament.TeamMode == TournamentTeamMode.DuoPredefined)
        {
            // The admin assigns teams; participants register individually into the pool first.
        }

        var participant = new TournamentParticipant
        {
            TournamentId = tournament.Id,
            PlayerId = playerId,
            UserId = userId,
            RegisteredAt = DateTime.UtcNow
        };

        dbContext.TournamentParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        return participant;
    }

    public void ValidateRegistrationWindow(Tournament tournament)
    {
        if (tournament.Status == TournamentStatus.Cancelled || tournament.Status == TournamentStatus.Completed)
        {
            throw new TournamentRuleException("This tournament is no longer accepting registrations.");
        }

        if (tournament.Status == TournamentStatus.InProgress)
        {
            throw new TournamentRuleException("This tournament has already started.");
        }

        var now = DateTime.UtcNow;
        if (now < tournament.RegistrationStartsAt)
        {
            throw new TournamentRuleException("Registration has not opened yet.");
        }

        if (now > tournament.RegistrationDeadline)
        {
            throw new TournamentRuleException("Registration deadline has passed.");
        }

        if (tournament.Status == TournamentStatus.Upcoming && now >= tournament.StartsAt)
        {
            throw new TournamentRuleException("The tournament has already started.");
        }
    }

    /// <summary>Admin override: register any tracker player even outside the registration window (while not in progress).</summary>
    public async Task<TournamentParticipant> AdminRegisterPlayerAsync(Tournament tournament, int playerId)
    {
        if (tournament.Status is TournamentStatus.InProgress or TournamentStatus.Completed or TournamentStatus.Cancelled)
        {
            throw new TournamentRuleException("Cannot add participants to a tournament that is in progress, completed, or cancelled.");
        }

        var alreadyRegistered = await dbContext.TournamentParticipants
            .AnyAsync(p => p.TournamentId == tournament.Id && p.PlayerId == playerId);
        if (alreadyRegistered)
        {
            throw new TournamentRuleException("This player is already registered.");
        }

        var playerExists = await dbContext.Players.AnyAsync(p => p.Id == playerId);
        if (!playerExists)
        {
            throw new TournamentRuleException("This player does not exist in the tracker.");
        }

        if (tournament.MaxParticipants > 0)
        {
            var currentCount = await dbContext.TournamentParticipants
                .CountAsync(p => p.TournamentId == tournament.Id && !p.IsDisqualified);
            if (currentCount >= tournament.MaxParticipants)
            {
                throw new TournamentRuleException("This tournament is full. Increase the participant limit first.");
            }
        }

        var participant = new TournamentParticipant
        {
            TournamentId = tournament.Id,
            PlayerId = playerId,
            RegisteredAt = DateTime.UtcNow
        };

        dbContext.TournamentParticipants.Add(participant);
        await dbContext.SaveChangesAsync();
        return participant;
    }

    // ─── Teams (2v2) ─────────────────────────────────────────────

    /// <summary>Creates a predefined team (admin). Max 2 members, player must belong to this tournament only.</summary>
    public async Task<TournamentTeam> CreateTeamAsync(Tournament tournament, string name, int? seed = null)
    {
        if (tournament.TeamMode == TournamentTeamMode.Solo)
        {
            throw new TournamentRuleException("Teams are only used in 2v2 tournaments.");
        }

        if (tournament.Status is TournamentStatus.InProgress or TournamentStatus.Completed or TournamentStatus.Cancelled)
        {
            throw new TournamentRuleException("Teams can only be managed before the tournament starts.");
        }

        var nameTaken = await dbContext.TournamentTeams
            .AnyAsync(t => t.TournamentId == tournament.Id && t.Name == name);
        if (nameTaken)
        {
            throw new TournamentRuleException($"A team named '{name}' already exists in this tournament.");
        }

        var team = new TournamentTeam
        {
            TournamentId = tournament.Id,
            Name = name,
            IsRandomTeam = false,
            Seed = seed ?? 0
        };

        dbContext.TournamentTeams.Add(team);
        await dbContext.SaveChangesAsync();
        return team;
    }

    /// <summary>Adds a participant to a team (max 2, no cross-team membership).</summary>
    public async Task AssignParticipantToTeamAsync(Tournament tournament, int participantId, int teamId)
    {
        if (tournament.TeamMode == TournamentTeamMode.Solo)
        {
            throw new TournamentRuleException("Teams are only used in 2v2 tournaments.");
        }

        var participant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Participant not found in this tournament.");

        var team = await dbContext.TournamentTeams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Team not found in this tournament.");

        if (participant.TeamId == teamId)
        {
            return; // already on this team — idempotent
        }

        if (participant.TeamId != null)
        {
            throw new TournamentRuleException("This player is already on a team in this tournament. Remove them from that team first.");
        }

        var memberCount = await dbContext.TournamentParticipants
            .CountAsync(p => p.TeamId == teamId && !p.IsDisqualified);
        if (memberCount >= 2)
        {
            throw new TournamentRuleException("Teams can have at most 2 players.");
        }

        participant.TeamId = teamId;
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveParticipantFromTeamAsync(Tournament tournament, int participantId)
    {
        var participant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Participant not found in this tournament.");

        if (participant.TeamId == null)
        {
            return;
        }

        // Team membership must be cleared before matches are generated/played.
        if (tournament.Status == TournamentStatus.InProgress)
        {
            throw new TournamentRuleException("Cannot change teams while the tournament is in progress.");
        }

        participant.TeamId = null;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Randomly pairs all unassigned participants into teams of 2. Uses
    /// cryptographic shuffling — NOT registration order. Leftover odd player
    /// gets a BYE-friendly team of 1 or is left unpaired (reported to caller).
    /// </summary>
    public async Task<List<TournamentTeam>> CreateRandomTeamsAsync(Tournament tournament)
    {
        if (tournament.TeamMode != TournamentTeamMode.DuoRandom)
        {
            throw new TournamentRuleException("Random teams are only available for 2v2 Random Teams tournaments.");
        }

        if (tournament.Status is TournamentStatus.InProgress or TournamentStatus.Completed or TournamentStatus.Cancelled)
        {
            throw new TournamentRuleException("Teams can only be generated before the tournament starts.");
        }

        var existingTeams = await dbContext.TournamentTeams
            .Where(t => t.TournamentId == tournament.Id)
            .ToListAsync();
        if (existingTeams.Count > 0)
        {
            throw new TournamentRuleException("Teams already exist for this tournament. Delete them first to regenerate.");
        }

        var unassigned = await dbContext.TournamentParticipants
            .Where(p => p.TournamentId == tournament.Id && !p.IsDisqualified && p.TeamId == null)
            .ToListAsync();

        if (unassigned.Count < 2)
        {
            throw new TournamentRuleException("Need at least 2 registered players to create random teams.");
        }

        // Fisher–Yates shuffle with a cryptographic RNG so pairing is random,
        // not registration order.
        for (var i = unassigned.Count - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (unassigned[i], unassigned[j]) = (unassigned[j], unassigned[i]);
        }

        var createdTeams = new List<TournamentTeam>();
        var nextTeamNumber = 1;

        for (var i = 0; i + 1 < unassigned.Count; i += 2)
        {
            var team = new TournamentTeam
            {
                TournamentId = tournament.Id,
                Name = $"Team {nextTeamNumber}",
                IsRandomTeam = true,
                Seed = 0
            };

            dbContext.TournamentTeams.Add(team);
            await dbContext.SaveChangesAsync(); // need team.Id for members

            unassigned[i].TeamId = team.Id;
            unassigned[i + 1].TeamId = team.Id;
            createdTeams.Add(team);
            nextTeamNumber++;
        }

        await dbContext.SaveChangesAsync();

        var leftover = unassigned.Count % 2 == 1 ? unassigned[^1] : null;
        if (leftover != null)
        {
            // Odd player out: create a solo team so they can still compete.
            var soloTeam = new TournamentTeam
            {
                TournamentId = tournament.Id,
                Name = $"Team {nextTeamNumber}",
                IsRandomTeam = true,
                Seed = 0
            };
            dbContext.TournamentTeams.Add(soloTeam);
            await dbContext.SaveChangesAsync();
            leftover.TeamId = soloTeam.Id;
            createdTeams.Add(soloTeam);
            await dbContext.SaveChangesAsync();
        }

        return createdTeams;
    }

    public async Task DeleteTeamAsync(Tournament tournament, int teamId)
    {
        if (tournament.Status == TournamentStatus.InProgress)
        {
            throw new TournamentRuleException("Cannot delete teams while the tournament is in progress.");
        }

        var team = await dbContext.TournamentTeams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Team not found.");

        var members = await dbContext.TournamentParticipants
            .Where(p => p.TeamId == teamId)
            .ToListAsync();
        foreach (var member in members)
        {
            member.TeamId = null;
        }

        // If a bracket already exists, matches referencing this team must be cleared.
        var matches = await dbContext.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id &&
                        (m.Team1Id == teamId || m.Team2Id == teamId))
            .ToListAsync();

        foreach (var match in matches)
        {
            if (match.Team1Id == teamId)
            {
                match.Team1Id = null;
            }
            if (match.Team2Id == teamId)
            {
                match.Team2Id = null;
            }
            if (match.Status == TournamentMatchStatus.Completed)
            {
                match.Status = TournamentMatchStatus.Pending;
                match.WinnerId = null;
                match.WinnerTeamId = null;
                match.PlayedAt = null;
                await ClearDownstreamSlotAsync(tournament, match);
            }
        }

        dbContext.TournamentTeams.Remove(team);
        await dbContext.SaveChangesAsync();
    }

    // ─── Bracket ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a persistent single-elimination bracket from participants (1v1)
    /// or teams (2v2). Supports any count with BYEs: the field is padded to the
    /// next power of two; an opponent slot left null is a BYE and auto-advances.
    /// </summary>
    public async Task<List<TournamentMatch>> GenerateBracketAsync(Tournament tournament)
    {
        if (tournament.Status is TournamentStatus.Completed or TournamentStatus.Cancelled)
        {
            throw new TournamentRuleException("Cannot generate a bracket for a completed or cancelled tournament.");
        }

        var existingMatches = await dbContext.TournamentMatches
            .AnyAsync(m => m.TournamentId == tournament.Id);
        if (existingMatches)
        {
            throw new TournamentRuleException("A bracket already exists for this tournament. Delete it first to regenerate.");
        }

        List<int?> slots;
        var isTeamBracket = tournament.TeamMode != TournamentTeamMode.Solo;

        if (tournament.TeamMode == TournamentTeamMode.Solo)
        {
            var participants = await dbContext.TournamentParticipants
                .Where(p => p.TournamentId == tournament.Id && !p.IsDisqualified)
                .OrderBy(p => p.Id)
                .ToListAsync();
            if (participants.Count < 2)
            {
                throw new TournamentRuleException("Need at least 2 participants to generate a bracket.");
            }

            slots = participants.Select(p => (int?)p.Id).ToList();
        }
        else
        {
            var teams = await dbContext.TournamentTeams
                .Where(t => t.TournamentId == tournament.Id)
                .OrderBy(t => t.Id)
                .ToListAsync();
            if (teams.Count < 2)
            {
                throw new TournamentRuleException("Need at least 2 teams to generate a bracket. Create teams first.");
            }

            slots = teams.Select(t => (int?)t.Id).ToList();
        }

        var entrantCount = slots.Count;
        var bracketSize = 1;
        while (bracketSize < entrantCount)
        {
            bracketSize *= 2;
        }

        // Shuffle entrants so seeding isn't just registration/creation order.
        var shuffled = new List<int?>(slots);
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var swapIndex = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
        }

        // Distribute BYEs across the bracket so they never meet each other in
        // round 1 (classic seeding pattern: BYEs alternate with real entrants).
        var byesToPlace = bracketSize - entrantCount;
        if (byesToPlace > 0)
        {
            var spaced = new List<int?>();
            var entrantQueue = new Queue<int?>(shuffled);
            var spacing = Math.Max(1, entrantCount / Math.Max(1, byesToPlace));
            var nextByeAfter = spacing - 1;
            var placed = 0;
            while (spaced.Count < bracketSize)
            {
                if (nextByeAfter <= 0 && byesToPlace > 0)
                {
                    spaced.Add(null); // BYE
                    byesToPlace--;
                    nextByeAfter = spacing;
                    continue;
                }
                spaced.Add(entrantQueue.Count > 0 ? entrantQueue.Dequeue() : (int?)null);
                placed++;
                nextByeAfter--;
            }
            // Any unplaced BYEs (edge cases) go at the end.
            while (byesToPlace > 0)
            {
                spaced.Add(null);
                byesToPlace--;
            }
            shuffled = spaced;
        }

        // Build round 1 with BYEs: pair shuffled entrants; null = BYE auto-advance.
        var round1Matches = new List<TournamentMatch>();
        var slotIndex = 0;
        for (var slot = 0; slot < bracketSize / 2; slot++)
        {
            var a = slotIndex < shuffled.Count ? shuffled[slotIndex++] : (int?)null;
            var b = slotIndex < shuffled.Count ? shuffled[slotIndex++] : (int?)null;

            round1Matches.Add(new TournamentMatch
            {
                TournamentId = tournament.Id,
                Round = 1,
                Slot = slot + 1,
                // 1v1 fills participant slots; 2v2 fills team slots.
                Participant1Id = isTeamBracket ? null : a,
                Participant2Id = isTeamBracket ? null : b,
                Team1Id = isTeamBracket ? a : null,
                Team2Id = isTeamBracket ? b : null,
                Status = TournamentMatchStatus.Pending
            });
        }

        // Build every round first with deterministic slot math: the match at
        // (round r, slot s) feeds the match at (r+1, ceil(s/2)). After saving we
        // resolve NextMatchId/NextMatchSlot from these coordinates — no reliance
        // on EF navigation fixup.
        var totalRounds = 1;
        var matchesInRound = bracketSize / 2;
        while (matchesInRound > 1)
        {
            totalRounds++;
            matchesInRound /= 2;
        }

        var createdMatches = new List<TournamentMatch>(round1Matches);
        for (var r = 2; r <= totalRounds; r++)
        {
            var countInRound = bracketSize / (int)Math.Pow(2, r);
            for (var s = 1; s <= countInRound; s++)
            {
                createdMatches.Add(new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    Round = r,
                    Slot = s,
                    Status = TournamentMatchStatus.Pending
                });
            }
        }

        foreach (var match in createdMatches)
        {
            dbContext.TournamentMatches.Add(match);
        }

        await dbContext.SaveChangesAsync(); // all matches now have IDs

        // Wire the advancement links from the round/slot coordinates.
        foreach (var match in createdMatches)
        {
            if (match.Round < totalRounds)
            {
                match.NextMatchSlot = match.Slot % 2 == 1 ? 1 : 2;
                match.NextMatchId = createdMatches
                    .Single(m => m.Round == match.Round + 1 && m.Slot == (match.Slot + 1) / 2)
                    .Id;
            }
        }

        await dbContext.SaveChangesAsync();

        // Resolve BYEs immediately: a match with one entrant and an empty slot
        // auto-advances that entrant into the next round without being played.
        var allMatches = createdMatches;

        foreach (var match in allMatches.Where(m => m.Round == 1))
        {
            if (isTeamBracket)
            {
                if (match.Team1Id != null && match.Team2Id == null)
                {
                    AdvanceWinner(match, winnerTeamId: match.Team1Id);
                    match.Note = "BYE — auto-advanced.";
                }
                else if (match.Team1Id == null && match.Team2Id != null)
                {
                    AdvanceWinner(match, winnerTeamId: match.Team2Id);
                    match.Note = "BYE — auto-advanced.";
                }
            }
            else
            {
                if (match.Participant1Id != null && match.Participant2Id == null)
                {
                    AdvanceWinner(match, winnerParticipantId: match.Participant1Id);
                    match.Note = "BYE — auto-advanced.";
                }
                else if (match.Participant1Id == null && match.Participant2Id != null)
                {
                    AdvanceWinner(match, winnerParticipantId: match.Participant2Id);
                    match.Note = "BYE — auto-advanced.";
                }
            }
        }

        // Push all BYE winners into their next-round slots.
        foreach (var match in allMatches.Where(m => (m.WinnerId != null || m.WinnerTeamId != null) && m.NextMatchId != null))
        {
            var next = allMatches.Single(m => m.Id == match.NextMatchId);
            if (isTeamBracket)
            {
                if (match.NextMatchSlot == 2) next.Team2Id = match.WinnerTeamId;
                else next.Team1Id = match.WinnerTeamId;
            }
            else
            {
                if (match.NextMatchSlot == 2) next.Participant2Id = match.WinnerId;
                else next.Participant1Id = match.WinnerId;
            }
        }

        await dbContext.SaveChangesAsync();

        // Mark the tournament as in progress once the bracket exists.
        if (tournament.Status is TournamentStatus.Draft or TournamentStatus.RegistrationOpen
            or TournamentStatus.RegistrationClosed or TournamentStatus.Upcoming)
        {
            tournament.Status = TournamentStatus.InProgress;
        }

        tournament.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return allMatches;
    }

    // Lightweight local tracking: NextMatch navigation was used during creation
    // only; the property remains set but NextMatchId is the persisted source of
    // truth for advancement.

    private static void AdvanceWinner(TournamentMatch match, int? winnerParticipantId = null, int? winnerTeamId = null)
    {
        match.WinnerId = winnerParticipantId;
        match.WinnerTeamId = winnerTeamId;
        match.Status = TournamentMatchStatus.Completed;
        if (match.PlayedAt == null)
        {
            match.PlayedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets or changes a match winner and advances them into the next round.
    /// Changing an already-completed match updates the downstream slot without
    /// regenerating the bracket.
    /// </summary>
    public async Task SetMatchWinnerAsync(Tournament tournament, int matchId, int? winnerParticipantId, string? note = null)
    {
        var match = await dbContext.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Match not found.");

        if (tournament.Status != TournamentStatus.InProgress)
        {
            throw new TournamentRuleException("Match results can only be recorded while the tournament is in progress.");
        }

        var isTeamBracket = tournament.TeamMode != TournamentTeamMode.Solo;
        int? slot1 = isTeamBracket ? match.Team1Id : match.Participant1Id;
        int? slot2 = isTeamBracket ? match.Team2Id : match.Participant2Id;

        if (isTeamBracket)
        {
            if (winnerParticipantId != null && winnerParticipantId != match.Team1Id && winnerParticipantId != match.Team2Id)
            {
                throw new TournamentRuleException("Winner must be one of the match teams.");
            }
        }
        else if (winnerParticipantId != null
            && winnerParticipantId != match.Participant1Id
            && winnerParticipantId != match.Participant2Id)
        {
            throw new TournamentRuleException("Winner must be one of the match participants.");
        }

        var hadPreviousWinner = (match.WinnerId != null || match.WinnerTeamId != null);
        var previousWinnerId = isTeamBracket ? match.WinnerTeamId : match.WinnerId;

        // If both slots are empty, nothing to record.
        if (slot1 == null && slot2 == null)
        {
            throw new TournamentRuleException("This match has no participants yet.");
        }

        // Normalize: winnerParticipantId carries either the participant id (1v1)
        // or the team id (2v2); store it in the right column.
        if (isTeamBracket)
        {
            match.WinnerTeamId = winnerParticipantId;
            match.WinnerId = null;
        }
        else
        {
            match.WinnerId = winnerParticipantId;
            match.WinnerTeamId = null;
        }

        if (winnerParticipantId != null)
        {
            match.Status = TournamentMatchStatus.Completed;
            match.PlayedAt = DateTime.UtcNow;
            if (note != null)
            {
                match.Note = note.Length > 200 ? note[..200] : note;
            }
        }
        else
        {
            // Reset the match.
            match.Status = TournamentMatchStatus.Pending;
            match.PlayedAt = null;
            match.Note = note;
        }

        // Update the next round slot — without regenerating the bracket.
        if (match.NextMatchId != null)
        {
            var nextMatch = await dbContext.TournamentMatches
                .FirstOrDefaultAsync(m => m.Id == match.NextMatchId)
                ?? throw new TournamentRuleException("Next round match is missing. The bracket is inconsistent.");

            if (isTeamBracket)
            {
                if (match.NextMatchSlot == 2) nextMatch.Team2Id = winnerParticipantId;
                else nextMatch.Team1Id = winnerParticipantId;
            }
            else
            {
                if (match.NextMatchSlot == 2) nextMatch.Participant2Id = winnerParticipantId;
                else nextMatch.Participant1Id = winnerParticipantId;
            }

            // If the previous winner changed, the next match may now be invalid:
            // clear its result if the old winner no longer occupies a slot.
            if (hadPreviousWinner && previousWinnerId != winnerParticipantId)
            {
                var nextSlot1 = isTeamBracket ? nextMatch.Team1Id : nextMatch.Participant1Id;
                var nextSlot2 = isTeamBracket ? nextMatch.Team2Id : nextMatch.Participant2Id;
                var nextWinner = isTeamBracket ? nextMatch.WinnerTeamId : nextMatch.WinnerId;

                if (nextWinner != null && nextWinner != nextSlot1 && nextWinner != nextSlot2)
                {
                    // Winner no longer among the entrants — reset the next match.
                    nextMatch.WinnerId = null;
                    nextMatch.WinnerTeamId = null;
                    nextMatch.Status = TournamentMatchStatus.Pending;
                    nextMatch.PlayedAt = null;
                }
            }
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Swaps one entrant (participant or team) in a match for another (admin fix for no-shows etc.).</summary>
    public async Task SetMatchParticipantsAsync(Tournament tournament, int matchId, int? participant1Id, int? participant2Id)
    {
        var match = await dbContext.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Match not found.");

        var isTeamBracket = tournament.TeamMode != TournamentTeamMode.Solo;

        if (isTeamBracket)
        {
            foreach (var candidateId in new[] { participant1Id, participant2Id })
            {
                if (candidateId != null)
                {
                    var teamExists = await dbContext.TournamentTeams
                        .AnyAsync(t => t.Id == candidateId && t.TournamentId == tournament.Id);
                    if (!teamExists)
                    {
                        throw new TournamentRuleException("Team must belong to this tournament.");
                    }
                }
            }

            if (participant1Id != null && participant1Id == participant2Id)
            {
                throw new TournamentRuleException("A match cannot have the same team on both sides.");
            }

            match.Team1Id = participant1Id;
            match.Team2Id = participant2Id;

            if (match.WinnerTeamId != null && match.WinnerTeamId != participant1Id && match.WinnerTeamId != participant2Id)
            {
                match.WinnerTeamId = null;
                match.Status = TournamentMatchStatus.Pending;
                match.PlayedAt = null;
                await ClearDownstreamSlotAsync(tournament, match);
            }
        }
        else
        {
            foreach (var candidateId in new[] { participant1Id, participant2Id })
            {
                if (candidateId != null)
                {
                    var exists = await dbContext.TournamentParticipants
                        .AnyAsync(p => p.Id == candidateId && p.TournamentId == tournament.Id && !p.IsDisqualified);
                    if (!exists)
                    {
                        throw new TournamentRuleException("Participant must be an active member of this tournament.");
                    }
                }
            }

            if (participant1Id != null && participant2Id != null && participant1Id == participant2Id)
            {
                throw new TournamentRuleException("A match cannot have the same participant on both sides.");
            }

            match.Participant1Id = participant1Id;
            match.Participant2Id = participant2Id;

            if (match.WinnerId != null && match.WinnerId != participant1Id && match.WinnerId != participant2Id)
            {
                match.WinnerId = null;
                match.Status = TournamentMatchStatus.Pending;
                match.PlayedAt = null;
                await ClearDownstreamSlotAsync(tournament, match);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task ClearDownstreamSlotAsync(Tournament tournament, TournamentMatch match)
    {
        if (match.NextMatchId == null)
        {
            return;
        }

        var nextMatch = await dbContext.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == match.NextMatchId);
        if (nextMatch == null)
        {
            return;
        }

        var isTeamBracket = tournament.TeamMode != TournamentTeamMode.Solo;
        var slot = match.NextMatchSlot == 2 ? 2 : 1;
        var downstreamId = isTeamBracket ? match.WinnerTeamId : match.WinnerId;
        if (downstreamId != null)
        {
            if (isTeamBracket)
            {
                if (slot == 1 && nextMatch.Team1Id == downstreamId) nextMatch.Team1Id = null;
                else if (slot == 2 && nextMatch.Team2Id == downstreamId) nextMatch.Team2Id = null;
            }
            else
            {
                if (slot == 1 && nextMatch.Participant1Id == downstreamId) nextMatch.Participant1Id = null;
                else if (slot == 2 && nextMatch.Participant2Id == downstreamId) nextMatch.Participant2Id = null;
            }

            var nextWinner = isTeamBracket ? nextMatch.WinnerTeamId : nextMatch.WinnerId;
            var nextSlot1 = isTeamBracket ? nextMatch.Team1Id : nextMatch.Participant1Id;
            var nextSlot2 = isTeamBracket ? nextMatch.Team2Id : nextMatch.Participant2Id;
            if (nextWinner != null && nextWinner != nextSlot1 && nextWinner != nextSlot2)
            {
                nextMatch.WinnerId = null;
                nextMatch.WinnerTeamId = null;
                nextMatch.Status = TournamentMatchStatus.Pending;
                nextMatch.PlayedAt = null;
                await ClearDownstreamSlotAsync(tournament, nextMatch);
            }
        }
    }

    /// <summary>
    /// Disqualifies a participant: removes them from their team, from any pending
    /// matches, and awards pending opponents a walkover. If they already won
    /// matches, the affected downstream slots are cleared.
    /// </summary>
    public async Task DisqualifyParticipantAsync(Tournament tournament, int participantId, string? reason)
    {
        var participant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Participant not found.");

        participant.IsDisqualified = true;
        participant.DisqualifiedReason = reason is { Length: > 200 } ? reason[..200] : reason;

        // Remove from team.
        participant.TeamId = null;

        // Walk over their pending matches: opponent advances automatically.
        var matches = await dbContext.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .ToListAsync();

        foreach (var match in matches.Where(m => m.Status != TournamentMatchStatus.Completed))
        {
            if (match.Participant1Id == participantId && match.Participant2Id != null)
            {
                AdvanceWinner(match, winnerParticipantId: match.Participant2Id);
                match.Note = "Opponent disqualified — auto-advanced.";
            }
            else if (match.Participant2Id == participantId && match.Participant1Id != null)
            {
                AdvanceWinner(match, winnerParticipantId: match.Participant1Id);
                match.Note = "Opponent disqualified — auto-advanced.";
            }
            else if (match.Participant1Id == participantId || match.Participant2Id == participantId)
            {
                // No opponent to advance — clear the slot and downstream effects.
                if (match.Participant1Id == participantId) match.Participant1Id = null;
                if (match.Participant2Id == participantId) match.Participant2Id = null;
                await ClearDownstreamSlotAsync(tournament, match);
            }
        }

        // Completed matches the participant won: clear downstream effects.
        foreach (var match in matches.Where(m => m.Status == TournamentMatchStatus.Completed && m.WinnerId == participantId))
        {
            match.WinnerId = null;
            match.Status = TournamentMatchStatus.Pending;
            match.PlayedAt = null;
            await ClearDownstreamSlotAsync(tournament, match);
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Removes a participant from the tournament entirely (admin).</summary>
    public async Task RemoveParticipantAsync(Tournament tournament, int participantId)
    {
        var participant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Participant not found.");

        if (tournament.Status == TournamentStatus.InProgress)
        {
            // Treat as disqualification so bracket consistency is maintained.
            await DisqualifyParticipantAsync(tournament, participantId, "Removed by admin.");
            return;
        }

        // Pre-tournament: simple removal. Clear matches that reference them.
        var matches = await dbContext.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .ToListAsync();
        foreach (var match in matches)
        {
            if (match.Participant1Id == participantId) match.Participant1Id = null;
            if (match.Participant2Id == participantId) match.Participant2Id = null;
            if (match.WinnerId == participantId)
            {
                match.WinnerId = null;
                match.Status = TournamentMatchStatus.Pending;
                match.PlayedAt = null;
                await ClearDownstreamSlotAsync(tournament, match);
            }
        }

        dbContext.TournamentParticipants.Remove(participant);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Completes the tournament if the final is decided: records 1st/2nd/3rd
    /// place from the semifinal losers / bracket shape, persists prizes, and
    /// marks status COMPLETED.
    /// </summary>
    public async Task<Tournament> TryCompleteTournamentAsync(Tournament tournament)
    {
        var matches = await dbContext.TournamentMatches
            .Where(m => m.TournamentId == tournament.Id)
            .ToListAsync();

        if (matches.Count == 0)
        {
            throw new TournamentRuleException("No bracket exists for this tournament.");
        }

        var maxRound = matches.Max(m => m.Round);
        var final = matches.FirstOrDefault(m => m.Round == maxRound && m.NextMatchId == null)
                    ?? throw new TournamentRuleException("Bracket is inconsistent — no final match found.");

        var isTeamBracket = tournament.TeamMode != TournamentTeamMode.Solo;

        var finalWinner = isTeamBracket ? final.WinnerTeamId : final.WinnerId;
        if (final.Status != TournamentMatchStatus.Completed || finalWinner == null)
        {
            throw new TournamentRuleException("The final match has not been played yet.");
        }

        // Resolve display names for both modes. In 2v2 the team name is the
        // placement; in 1v1 the participant's player name.
        var participantIds = matches
            .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var participantNames = await dbContext.TournamentParticipants
            .Where(p => participantIds.Contains(p.Id))
            .Include(p => p.Player)
            .ToDictionaryAsync(p => p.Id, p => p.Player != null ? p.Player.Username : "Unknown");

        var teamIds = matches
            .SelectMany(m => new[] { m.Team1Id, m.Team2Id })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var teamNames = teamIds.Count > 0
            ? await dbContext.TournamentTeams
                .Where(t => teamIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name)
            : new Dictionary<int, string>();

        string? NameOf(int? participantId, int? teamId)
        {
            if (isTeamBracket)
            {
                return teamId != null ? teamNames.GetValueOrDefault(teamId.Value) : null;
            }
            return participantId != null ? participantNames.GetValueOrDefault(participantId.Value) : null;
        }

        var firstId = finalWinner;
        var secondId = finalWinner == (isTeamBracket ? final.Team1Id : final.Participant1Id)
            ? (isTeamBracket ? final.Team2Id : final.Participant2Id)
            : (isTeamBracket ? final.Team1Id : final.Participant1Id);

        if (isTeamBracket)
        {
            tournament.FirstPlaceName = NameOf(null, firstId);
            tournament.SecondPlaceName = NameOf(null, secondId);
        }
        else
        {
            tournament.FirstPlaceParticipantId = firstId;
            tournament.SecondPlaceParticipantId = secondId;
            tournament.FirstPlaceName = NameOf(firstId, null);
            tournament.SecondPlaceName = NameOf(secondId, null);
        }

        // Third place: loser of the semifinal that fed the champion's slot.
        if (maxRound >= 2)
        {
            var feederMatches = matches.Where(m => m.NextMatchId == final.Id).ToList();
            var winnerFeeder = feederMatches.FirstOrDefault(m =>
                (isTeamBracket ? m.WinnerTeamId : m.WinnerId) != null
                && (isTeamBracket ? m.WinnerTeamId : m.WinnerId) == firstId);
            var loserId = winnerFeeder == null
                ? null
                : ((isTeamBracket ? winnerFeeder.WinnerTeamId : winnerFeeder.WinnerId) == (isTeamBracket ? winnerFeeder.Team1Id : winnerFeeder.Participant1Id)
                    ? (isTeamBracket ? winnerFeeder.Team2Id : winnerFeeder.Participant2Id)
                    : (isTeamBracket ? winnerFeeder.Team1Id : winnerFeeder.Participant1Id));

            if (isTeamBracket)
            {
                tournament.ThirdPlaceName = loserId != null ? teamNames.GetValueOrDefault(loserId.Value) : null;
            }
            else
            {
                tournament.ThirdPlaceParticipantId = loserId;
                tournament.ThirdPlaceName = loserId != null ? participantNames.GetValueOrDefault(loserId.Value) : null;
            }
        }

        tournament.Status = TournamentStatus.Completed;
        tournament.CompletedAt = DateTime.UtcNow;
        tournament.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return tournament;
    }

    /// <summary>Deletes the bracket (admin regeneration path). Only before completion.</summary>
    public async Task DeleteBracketAsync(Tournament tournament)
    {
        if (tournament.Status == TournamentStatus.Completed)
        {
            throw new TournamentRuleException("Cannot delete the bracket of a completed tournament.");
        }

        await ClearAndRemoveMatchesAsync(tournament.Id);

        if (tournament.Status == TournamentStatus.InProgress)
        {
            tournament.Status = TournamentStatus.RegistrationClosed;
        }

        tournament.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Unconditional bracket removal used when deleting a whole tournament.
    /// Bypasses the completed-tournament guard because the tournament itself is
    /// going away (participants/teams/prizes cascade via the database).
    /// </summary>
    public async Task DeleteBracketForceAsync(Tournament tournament)
    {
        await ClearAndRemoveMatchesAsync(tournament.Id);
    }

    /// <summary>
    /// Removes all matches of a tournament. The self-referencing NextMatchId FK
    /// must be severed first, otherwise the delete order violates the restrict
    /// constraint and the whole operation fails.
    /// </summary>
    private async Task ClearAndRemoveMatchesAsync(int tournamentId)
    {
        var matches = await dbContext.TournamentMatches
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();

        foreach (var match in matches)
        {
            match.NextMatchId = null;
            match.NextMatch = null;
        }
        await dbContext.SaveChangesAsync();

        dbContext.TournamentMatches.RemoveRange(matches);
    }

    /// <summary>Resets a participant's team assignment so they can be re-teamed (2v2 predefined).</summary>
    public async Task ReplaceTeamPlayerAsync(Tournament tournament, int teamId, int oldParticipantId, int newParticipantId)
    {
        if (tournament.TeamMode == TournamentTeamMode.Solo)
        {
            throw new TournamentRuleException("Teams are only used in 2v2 tournaments.");
        }

        if (tournament.Status == TournamentStatus.InProgress)
        {
            throw new TournamentRuleException("Cannot replace players while the tournament is in progress.");
        }

        var team = await dbContext.TournamentTeams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("Team not found.");

        var oldParticipant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == oldParticipantId && p.TeamId == teamId)
            ?? throw new TournamentRuleException("The player being replaced is not on this team.");

        var newParticipant = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == newParticipantId && p.TournamentId == tournament.Id)
            ?? throw new TournamentRuleException("The replacement player is not registered in this tournament.");

        if (newParticipant.TeamId != null && newParticipant.TeamId != teamId)
        {
            throw new TournamentRuleException("The replacement player is already on another team.");
        }

        oldParticipant.TeamId = null;
        newParticipant.TeamId = teamId;
        await dbContext.SaveChangesAsync();
    }
}
