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
    /// <summary>
    /// Import backup playtime data from SQLite backup.
    /// Accepts daily playtime records and player totals.
    /// </summary>
    [HttpPost("import-playtime")]
    public async Task<IActionResult> ImportPlaytime([FromBody] ImportPlaytimeRequest request)
    {
        if (request.DailyPlaytime is null || request.DailyPlaytime.Count == 0)
        {
            return BadRequest(new { message = "No daily playtime records provided." });
        }

        // Batch-load all players into memory (map RobloxUserId -> Player)
        var allPlayers = await dbContext.Players.ToListAsync();
        var playerByRobloxId = allPlayers.ToDictionary(p => p.RobloxUserId);

        // Batch-load all existing daily playtime into memory
        var allDailyRows = await dbContext.DailyPlaytime.ToListAsync();
        var existingByPlayerDate = allDailyRows
            .GroupBy(d => d.PlayerId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(d => d.Date, d => d));

        var importedCount = 0;
        var skippedCount = 0;

        foreach (var record in request.DailyPlaytime)
        {
            if (!playerByRobloxId.TryGetValue(record.RobloxUserId, out var player))
            {
                skippedCount++;
                continue;
            }

            var date = DateOnly.Parse(record.Date);

            if (!existingByPlayerDate.TryGetValue(player.Id, out var dateMap))
            {
                dateMap = new Dictionary<DateOnly, DailyPlaytime>();
                existingByPlayerDate[player.Id] = dateMap;
            }

            if (!dateMap.TryGetValue(date, out var existing))
            {
                var newRow = new DailyPlaytime
                {
                    PlayerId = player.Id,
                    Date = date,
                    PlaySeconds = record.PlaySeconds
                };
                dbContext.DailyPlaytime.Add(newRow);
                dateMap[date] = newRow;
                importedCount++;
            }
            else if (existing.PlaySeconds < record.PlaySeconds)
            {
                existing.PlaySeconds = record.PlaySeconds;
                importedCount++;
            }
        }

        // Recalculate TotalPlaySeconds for affected players from in-memory data
        var affectedRobloxIds = request.DailyPlaytime.Select(r => r.RobloxUserId).Distinct().ToList();
        foreach (var robloxId in affectedRobloxIds)
        {
            if (!playerByRobloxId.TryGetValue(robloxId, out var player)) continue;
            if (!existingByPlayerDate.TryGetValue(player.Id, out var dateMap)) continue;
            player.TotalPlaySeconds = dateMap.Values.Sum(d => d.PlaySeconds);
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            imported = importedCount,
            skipped = skippedCount,
            playersUpdated = affectedRobloxIds.Count
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

    /// <summary>
    /// Admin: full user detail including Discord link and linked tracker player.
    /// </summary>
    [HttpGet("users/{id:int}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetUserDetail(int id)
    {
        var user = await dbContext.Users
            .Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(new AdminUserDetailDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            DiscordUserId = user.DiscordUserId,
            PlayerId = user.PlayerId,
            PlayerUsername = user.Player?.Username,
            PlayerRobloxUserId = user.Player?.RobloxUserId
        });
    }

    /// <summary>
    /// Admin: update a user's profile information (username, role, Discord link,
    /// linked tracker player). Used to manage accounts when necessary.
    /// </summary>
    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, AdminUpdateUserRequest request)
    {
        var user = await dbContext.Users
            .Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var username = (request.Username ?? string.Empty).Trim();
        if (username.Length < 3 || username.Length > 50)
        {
            return BadRequest(new { message = "Username must be between 3 and 50 characters." });
        }

        if (await dbContext.Users.AnyAsync(u => u.Id != id && u.Username == username))
        {
            return Conflict(new { message = $"Username '{username}' is already taken." });
        }

        var validRoles = new[] { "User", "Admin" };
        if (!validRoles.Contains(request.Role))
        {
            return BadRequest(new { message = "Invalid role. Must be User or Admin." });
        }

        var currentUsername = User.Identity?.Name;
        if (user.Role == "Admin" && request.Role != "Admin" && user.Username == currentUsername)
        {
            return BadRequest(new { message = "You cannot demote your own account." });
        }

        var discordUserId = (request.DiscordUserId ?? string.Empty).Trim();
        if (discordUserId.Length > 0
            && (discordUserId.Length is < 17 or > 20 || !discordUserId.All(char.IsAsciiDigit)))
        {
            return BadRequest(new { message = "Discord User ID must contain 17 to 20 digits." });
        }

        if (discordUserId.Length > 0
            && await dbContext.Users.AnyAsync(u => u.Id != id && u.DiscordUserId == discordUserId))
        {
            return Conflict(new { message = "That Discord account is already linked to another website account." });
        }

        // Player link: only allowed when the target player exists and is unclaimed.
        if (request.PlayerId is not null)
        {
            var player = await dbContext.Players.FindAsync(request.PlayerId.Value);
            if (player is null)
            {
                return BadRequest(new { message = "Tracker player not found." });
            }

            var claimedBy = await dbContext.Users.FirstOrDefaultAsync(u => u.PlayerId == player.Id && u.Id != id);
            if (claimedBy is not null)
            {
                return Conflict(new { message = $"That tracker player is already linked to account '{claimedBy.Username}'." });
            }
        }

        user.Username = username;
        user.Role = request.Role;
        user.DiscordUserId = discordUserId.Length == 0 ? null : discordUserId;

        // Keep the previously linked player and the newly linked one consistent.
        var previousPlayerId = user.PlayerId;
        user.PlayerId = request.PlayerId;
        if (previousPlayerId != user.PlayerId)
        {
            if (user.Player is not null && user.Player.DiscordUserId == discordUserId)
            {
                user.Player.DiscordUserId = null;
            }
        }

        if (user.PlayerId is not null)
        {
            var linkedPlayer = await dbContext.Players.FindAsync(user.PlayerId.Value);
            if (linkedPlayer is not null)
            {
                if (user.DiscordUserId is not null)
                {
                    linkedPlayer.DiscordUserId = user.DiscordUserId;
                }
                linkedPlayer.UpdatedAt = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "User updated." });
    }
}
