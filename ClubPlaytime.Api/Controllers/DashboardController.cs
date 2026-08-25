using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(IPlayerStatsService playerStatsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DashboardPlayerDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var players = await playerStatsService.GetDashboardAsync(cancellationToken);
        return Ok(players);
    }

    [HttpGet("leaderboard/weekly")]
    public async Task<ActionResult<IReadOnlyList<WeeklyLeaderboardDto>>> GetWeeklyLeaderboard(CancellationToken cancellationToken)
    {
        var leaderboard = await playerStatsService.GetWeeklyLeaderboardAsync(cancellationToken);
        return Ok(leaderboard);
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<IReadOnlyList<LeaderboardPlayerDto>>> GetLeaderboard(
        CancellationToken cancellationToken,
        [FromQuery] string period = "daily")
    {
        try
        {
            var leaderboard = await playerStatsService.GetLeaderboardAsync(period, cancellationToken);
            return Ok(leaderboard);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
