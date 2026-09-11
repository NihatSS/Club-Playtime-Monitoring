using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfileController(
    ClubPlaytimeDbContext dbContext,
    IPlayerStatsService playerStatsService) : ControllerBase
{
    /// <summary>
    /// Everything the profile page needs in one call: account info, linked
    /// Roblox tracker player (with playtime stats), Discord link state,
    /// leaderboard positions, and the user's most recent join request.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var currentUsername = User.Identity?.Name;
        var user = await dbContext.Users
            .Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Username == currentUsername, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var response = new MyProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            DiscordUserId = user.DiscordUserId
        };

        if (user.PlayerId is not null)
        {
            var details = await playerStatsService.GetPlayerDetailsAsync(user.PlayerId.Value, cancellationToken);
            if (details is not null)
            {
                response.Player = new ProfilePlayerDto
                {
                    Id = details.Id,
                    Username = details.Username,
                    RobloxUserId = details.RobloxUserId,
                    AvatarUrl = details.AvatarUrl,
                    Club = details.Club,
                    TodayPlaySeconds = details.TodayPlaySeconds,
                    WeeklyPlaySeconds = details.WeeklyPlaySeconds,
                    MonthlyPlaySeconds = details.MonthlyPlaySeconds,
                    TotalPlaySeconds = details.TotalPlaySeconds,
                    ProfileUrl = details.ProfileUrl
                };

                (response.WeeklyLeaderboardPosition, response.TotalLeaderboardPosition) =
                    await ComputeLeaderboardPositionsAsync(user.PlayerId.Value, cancellationToken);
            }
        }

        var joinRequest = await dbContext.JoinRequests
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        if (joinRequest is not null)
        {
            response.JoinRequest = new ProfileJoinRequestDto
            {
                Id = joinRequest.Id,
                RobloxUsername = joinRequest.RobloxUsername,
                RobloxUserId = joinRequest.RobloxUserId,
                DiscordUserId = joinRequest.DiscordUserId,
                Club = joinRequest.Club,
                Status = joinRequest.Status,
                Note = joinRequest.Note,
                CreatedAt = joinRequest.CreatedAt,
                ReviewedAt = joinRequest.ReviewedAt,
                ReviewedBy = joinRequest.ReviewedBy
            };
        }

        return Ok(response);
    }

    /// <summary>
    /// Link or unlink the current user's Discord account. Linking validates the
    /// snowflake and ensures no other user/player already holds it; unlinking
    /// also clears it from the linked tracker player so the bot stops matching.
    /// </summary>
    [HttpPost("discord")]
    public async Task<IActionResult> UpdateDiscord(UpdateDiscordRequest request, CancellationToken cancellationToken)
    {
        var currentUsername = User.Identity?.Name;
        var user = await dbContext.Users
            .Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Username == currentUsername, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var discordUserId = (request.DiscordUserId ?? string.Empty).Trim();

        if (discordUserId.Length == 0)
        {
            // Unlink: also clear from the linked tracker player so /playtime stops resolving.
            user.DiscordUserId = null;
            if (user.Player is not null && user.Player.DiscordUserId is not null)
            {
                user.Player.DiscordUserId = null;
                user.Player.UpdatedAt = DateTime.UtcNow;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Discord account unlinked.", linked = false });
        }

        if (!IsDiscordUserId(discordUserId))
        {
            return BadRequest(new { message = "Discord User ID must contain 17 to 20 digits. Enable Discord Developer Mode and use Copy User ID." });
        }

        var takenByUser = await dbContext.Users
            .AnyAsync(u => u.Id != user.Id && u.DiscordUserId == discordUserId, cancellationToken);
        if (takenByUser)
        {
            return Conflict(new { message = "That Discord account is already linked to another website account." });
        }

        var takenByPlayer = await dbContext.Players
            .AnyAsync(p => p.DiscordUserId == discordUserId && (user.PlayerId == null || p.Id != user.PlayerId), cancellationToken);
        if (takenByPlayer)
        {
            return Conflict(new { message = "That Discord account is already linked to a different tracker player." });
        }

        user.DiscordUserId = discordUserId;
        if (user.Player is not null)
        {
            user.Player.DiscordUserId = discordUserId;
            user.Player.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Discord account linked.", linked = true, discordUserId });
    }

    private async Task<(int? weekly, int? total)> ComputeLeaderboardPositionsAsync(int playerId, CancellationToken cancellationToken)
    {
        var weekFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6);

        var weeklyTotals = await dbContext.DailyPlaytime
            .Where(d => d.Date >= weekFrom)
            .GroupBy(d => d.PlayerId)
            .Select(g => new { PlayerId = g.Key, Total = g.Sum(d => d.PlaySeconds) })
            .ToDictionaryAsync(g => g.PlayerId, g => g.Total, cancellationToken);

        int? weekly = weeklyTotals.TryGetValue(playerId, out var weeklySeconds)
            ? 1 + weeklyTotals.Values.Count(v => v > weeklySeconds)
            : null;

        var totals = await dbContext.Players
            .Select(p => new { p.Id, p.TotalPlaySeconds })
            .ToListAsync(cancellationToken);
        var mine = totals.FirstOrDefault(t => t.Id == playerId);
        int? total = mine is null || mine.TotalPlaySeconds <= 0
            ? null
            : 1 + totals.Count(t => t.TotalPlaySeconds > mine.TotalPlaySeconds);

        return (weekly, total);
    }

    private static bool IsDiscordUserId(string value) =>
        value.Length is >= 17 and <= 20 && value.All(char.IsAsciiDigit);
}
