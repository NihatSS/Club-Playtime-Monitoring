using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

/// <summary>
/// Player statistics, streaks and achievements. Reads are public (consistent
/// with the existing public tracker data); admin-only endpoints add the
/// maintenance and per-user visibility required by the admin panel.
/// </summary>
[ApiController]
[Route("api/stats")]
public sealed class StatsController(
    ClubPlaytimeDbContext dbContext,
    PlayerProgressService progressService) : ControllerBase
{
    /// <summary>Public: full statistics for a tracker player (streaks, ranks, charts).</summary>
    [HttpGet("players/{playerId:int}")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<ActionResult<PlayerStatsDto>> GetPlayerStats(int playerId, CancellationToken cancellationToken)
    {
        var stats = await progressService.GetStatsAsync(playerId, cancellationToken);
        return stats is null ? NotFound() : Ok(stats);
    }

    /// <summary>Public: all achievements for a tracker player with progress.</summary>
    [HttpGet("players/{playerId:int}/achievements")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<ActionResult<PlayerAchievementsDto>> GetPlayerAchievements(int playerId, CancellationToken cancellationToken)
    {
        var achievements = await progressService.GetAchievementsAsync(playerId, cancellationToken);
        return achievements is null ? NotFound() : Ok(achievements);
    }

    /// <summary>
    /// Public: the signed-in user's own stats+achievements, resolved through
    /// their linked tracker player. Used by the profile page.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<PlayerStatsDto>> GetMyStats(CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name;
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user?.PlayerId is null)
        {
            return NotFound(new { message = "No tracker player linked to this account." });
        }

        var stats = await progressService.GetStatsAsync(user.PlayerId.Value, cancellationToken);
        return stats is null ? NotFound() : Ok(stats);
    }

    /// <summary>Admin: recompute all streaks and re-evaluate achievements (idempotent maintenance).</summary>
    [HttpPost("recompute")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecomputeAll(CancellationToken cancellationToken)
    {
        await progressService.UpdateAllPlayersProgressAsync(cancellationToken);
        return Ok(new { message = "Streaks and achievements recomputed for all players." });
    }
}
