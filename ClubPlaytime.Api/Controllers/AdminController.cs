using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(ClubPlaytimeDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// One-time fix: cap DailyPlaytime to max 24h per day and recalculate TotalPlaySeconds.
    /// This corrects inflated playtime caused by the LastSeenPlaying bug.
    /// </summary>
    [HttpPost("fix-playtime")]
    public async Task<IActionResult> FixPlaytime()
    {
        const long maxDailySeconds = 86_400; // 24 hours
        var dailyRows = await dbContext.DailyPlaytime.ToListAsync();
        var cappedCount = 0;
        var playersAffected = new HashSet<int>();

        foreach (var row in dailyRows)
        {
            if (row.PlaySeconds > maxDailySeconds)
            {
                row.PlaySeconds = maxDailySeconds;
                cappedCount++;
                playersAffected.Add(row.PlayerId);
            }
        }

        // Recalculate TotalPlaySeconds for all players from their daily records
        var playerTotals = await dbContext.DailyPlaytime
            .GroupBy(d => d.PlayerId)
            .Select(g => new { PlayerId = g.Key, Total = g.Sum(d => d.PlaySeconds) })
            .ToListAsync();

        var players = await dbContext.Players.ToListAsync();
        var recalcCount = 0;

        foreach (var player in players)
        {
            var correctTotal = playerTotals.FirstOrDefault(t => t.PlayerId == player.Id)?.Total ?? 0;
            if (player.TotalPlaySeconds != correctTotal)
            {
                player.TotalPlaySeconds = correctTotal;
                recalcCount++;
                playersAffected.Add(player.Id);
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            dailyRecordsCapped = cappedCount,
            playersRecalculated = recalcCount,
            totalPlayersAffected = playersAffected.Count
        });
    }
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers()
    {
        var users = await dbContext.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        if (await dbContext.Users.AnyAsync(u => u.Username == request.Username))
        {
            return Conflict(new { message = $"Username '{request.Username}' already exists." });
        }

        var validRoles = new[] { "User", "Admin" };
        if (!validRoles.Contains(request.Role))
        {
            return BadRequest(new { message = $"Invalid role. Must be one of: {string.Join(", ", validRoles)}" });
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // Prevent deleting yourself
        var currentUsername = User.Identity?.Name;
        if (user.Username == currentUsername)
        {
            return BadRequest(new { message = "You cannot delete your own account." });
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("users/{id:int}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, ChangePasswordRequest request)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }
}
