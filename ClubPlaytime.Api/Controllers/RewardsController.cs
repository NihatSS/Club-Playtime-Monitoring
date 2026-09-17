using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

public sealed class MonthlyRewardDto
{
    public string Prize { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public List<LeaderboardPlayerDto> TopPlayers { get; set; } = new();
}

public sealed class UpdateMonthlyRewardRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(300)]
    public string Prize { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public sealed class RewardsController(ClubPlaytimeDbContext dbContext, IPlayerStatsService playerStatsService) : ControllerBase
{
    /// <summary>
    /// Public: the reward for the player with the most playtime in the current
    /// calendar month — the prize plus the current top 10.
    /// </summary>
    [HttpGet("monthly")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<ActionResult<MonthlyRewardDto>> GetMonthlyReward(CancellationToken cancellationToken)
    {
        // Read-only fast path: never create the singleton row here. The old code
        // wrote on every public read AND raced — two concurrent first loads both
        // inserted the Id=1 row and one 500ed on the unique constraint.
        var setting = await dbContext.MonthlyRewardSetting
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == 1, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var topPlayers = (await playerStatsService.GetLeaderboardAsync("monthly", cancellationToken))
            .Take(10)
            .ToList();

        return Ok(new MonthlyRewardDto
        {
            Prize = setting?.Prize ?? string.Empty,
            UpdatedAt = setting?.UpdatedAt ?? utcNow,
            UpdatedBy = setting?.UpdatedBy,
            Month = utcNow.Month,
            Year = utcNow.Year,
            TopPlayers = topPlayers
        });
    }

    /// <summary>Admin: edit the monthly reward prize.</summary>
    [HttpPut("monthly")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMonthlyReward(UpdateMonthlyRewardRequest request, CancellationToken cancellationToken)
    {
        var prize = (request.Prize ?? string.Empty).Trim();
        if (prize.Length == 0)
        {
            return BadRequest(new { message = "Prize is required." });
        }
        if (prize.Length > 300)
        {
            prize = prize[..300];
        }

        var setting = await dbContext.MonthlyRewardSetting.FirstOrDefaultAsync(r => r.Id == 1, cancellationToken);
        if (setting is null)
        {
            // Singleton row — create on first admin save. If two admins save at the
            // same instant on a fresh DB, the loser retries as an update instead of 500ing.
            setting = new MonthlyRewardSetting { Id = 1, Prize = prize, UpdatedAt = DateTime.UtcNow, UpdatedBy = User.Identity?.Name };
            dbContext.MonthlyRewardSetting.Add(setting);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch (DbUpdateException)
            {
                dbContext.Entry(setting).State = EntityState.Detached;
                setting = await dbContext.MonthlyRewardSetting.FirstAsync(r => r.Id == 1, cancellationToken);
            }
        }

        setting.Prize = prize;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = User.Identity?.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
