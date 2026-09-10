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
public sealed class JoinRequestController(ClubPlaytimeDbContext dbContext, IPlayerStatsService playerStatsService) : ControllerBase
{
    /// <summary>
    /// Public: Submit a request to be added to the tracker.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<JoinRequestDto>> SubmitRequest(SubmitJoinRequest request)
    {
        var username = (request.RobloxUsername ?? string.Empty).Trim();
        var discordUserId = (request.DiscordUserId ?? string.Empty).Trim();

        if (!IsDiscordUserId(discordUserId))
        {
            return BadRequest(new { message = "Discord User ID must contain 17 to 20 digits. Enable Discord Developer Mode and use Copy User ID." });
        }

        // One request per user: a user can only have one non-declined request
        // (pending, or already approved and waiting to be added to the tracker)
        var existingRequest = await dbContext.JoinRequests
            .FirstOrDefaultAsync(r => r.RobloxUserId == request.RobloxUserId && r.Status != "Rejected");

        if (existingRequest is not null)
        {
            return Ok(new JoinRequestDto
            {
                Id = existingRequest.Id,
                RobloxUsername = existingRequest.RobloxUsername,
                RobloxUserId = existingRequest.RobloxUserId,
                DiscordUserId = existingRequest.DiscordUserId,
                Club = existingRequest.Club,
                Status = existingRequest.Status,
                Note = existingRequest.Note,
                CreatedAt = existingRequest.CreatedAt,
                ReviewedAt = existingRequest.ReviewedAt,
                ReviewedBy = existingRequest.ReviewedBy
            });
        }

        // Check if user is already tracked
        var alreadyTracked = await dbContext.Players
            .AnyAsync(p => p.RobloxUserId == request.RobloxUserId);

        if (alreadyTracked)
        {
            return Conflict(new { message = "This player is already being tracked." });
        }

        // Username, Roblox user ID and Discord ID must each be unique among
        // pending requests and tracked players
        var usernameInUse = await dbContext.JoinRequests
                .AnyAsync(r => r.Status == "Pending" && r.RobloxUsername.ToLower() == username.ToLower())
            || await dbContext.Players
                .AnyAsync(p => p.Username.ToLower() == username.ToLower());

        if (usernameInUse)
        {
            return Conflict(new { message = "This Roblox username is already in use." });
        }

        var discordIdInUse = await dbContext.JoinRequests
                .AnyAsync(r => r.Status == "Pending" && r.DiscordUserId == discordUserId)
            || await dbContext.Players
                .AnyAsync(p => p.DiscordUserId == discordUserId);

        if (discordIdInUse)
        {
            return Conflict(new { message = "This Discord ID is already in use." });
        }

        var joinRequest = new JoinRequest
        {
            RobloxUsername = username,
            RobloxUserId = request.RobloxUserId,
            DiscordUserId = discordUserId,
            Club = request.Club,
            Note = request.Note,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.JoinRequests.Add(joinRequest);
        await dbContext.SaveChangesAsync();

        return Created("", new JoinRequestDto
        {
            Id = joinRequest.Id,
            RobloxUsername = joinRequest.RobloxUsername,
            RobloxUserId = joinRequest.RobloxUserId,
            DiscordUserId = joinRequest.DiscordUserId,
            Club = joinRequest.Club,
            Status = joinRequest.Status,
            Note = joinRequest.Note,
            CreatedAt = joinRequest.CreatedAt
        });
    }

    /// <summary>
    /// Public: Look up a user's existing request by Roblox user ID.
    /// </summary>
    [HttpGet("mine")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyRequest([FromQuery] long robloxUserId)
    {
        if (robloxUserId <= 0)
        {
            return BadRequest(new { message = "A valid robloxUserId is required." });
        }

        var joinRequest = await dbContext.JoinRequests
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(r => r.RobloxUserId == robloxUserId);

        if (joinRequest is null)
        {
            return Ok(new JoinRequestExistsDto { Exists = false });
        }

        return Ok(new
        {
            Exists = true,
            Request = new JoinRequestDto
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
            }
        });
    }

    /// <summary>
    /// Public: Edit a pending request (users can fix mistakes).
    /// </summary>
    [HttpPut("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRequest(int id, UpdateJoinRequest request)
    {
        var joinRequest = await dbContext.JoinRequests.FindAsync(id);
        if (joinRequest is null)
        {
            return NotFound(new { message = "Request not found." });
        }

        if (joinRequest.Status != "Pending")
        {
            return Conflict(new { message = "Only requests that are still waiting for review can be edited." });
        }

        if (joinRequest.RobloxUserId != request.RobloxUserId)
        {
            return BadRequest(new { message = "The Roblox user ID cannot be changed. Submit a new request instead." });
        }

        var username = (request.RobloxUsername ?? string.Empty).Trim();
        var discordUserId = (request.DiscordUserId ?? string.Empty).Trim();

        if (!IsDiscordUserId(discordUserId))
        {
            return BadRequest(new { message = "Discord User ID must contain 17 to 20 digits. Enable Discord Developer Mode and use Copy User ID." });
        }

        var usernameInUse = await dbContext.JoinRequests
                .AnyAsync(r => r.Id != id && r.Status == "Pending" && r.RobloxUsername.ToLower() == username.ToLower())
            || await dbContext.Players
                .AnyAsync(p => p.Username.ToLower() == username.ToLower());

        if (usernameInUse)
        {
            return Conflict(new { message = "This Roblox username is already in use." });
        }

        var discordIdInUse = await dbContext.JoinRequests
                .AnyAsync(r => r.Id != id && r.Status == "Pending" && r.DiscordUserId == discordUserId)
            || await dbContext.Players
                .AnyAsync(p => p.DiscordUserId == discordUserId);

        if (discordIdInUse)
        {
            return Conflict(new { message = "This Discord ID is already in use." });
        }

        joinRequest.RobloxUsername = username;
        joinRequest.DiscordUserId = discordUserId;
        joinRequest.Club = request.Club;
        joinRequest.Note = request.Note;

        await dbContext.SaveChangesAsync();

        return Ok(new JoinRequestDto
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
        });
    }

    /// <summary>
    /// Admin: List all join requests.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<JoinRequestDto>>> GetRequests(
        [FromQuery] string? status = null)
    {
        var query = dbContext.JoinRequests.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new JoinRequestDto
            {
                Id = r.Id,
                RobloxUsername = r.RobloxUsername,
                RobloxUserId = r.RobloxUserId,
                DiscordUserId = r.DiscordUserId,
                Club = r.Club,
                Status = r.Status,
                Note = r.Note,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewedBy = r.ReviewedBy
            })
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// Admin: Approve or reject a join request.
    /// </summary>
    [HttpPut("{id:int}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewRequest(int id, ReviewJoinRequest review)
    {
        if (review.Status != "Approved" && review.Status != "Rejected")
        {
            return BadRequest(new { message = "Status must be 'Approved' or 'Rejected'." });
        }

        var joinRequest = await dbContext.JoinRequests.FindAsync(id);
        if (joinRequest is null)
        {
            return NotFound();
        }

        if (joinRequest.Status != "Pending")
        {
            return BadRequest(new { message = "This request has already been reviewed." });
        }

        joinRequest.Status = review.Status;
        joinRequest.ReviewedAt = DateTime.UtcNow;
        joinRequest.ReviewedBy = User.Identity?.Name;

        var message = $"Request {review.Status.ToLower()}.";

        // Approving a request automatically adds the player to the tracker
        if (review.Status == "Approved")
        {
            var alreadyTracked = await dbContext.Players
                .AnyAsync(p => p.RobloxUserId == joinRequest.RobloxUserId);

            if (alreadyTracked)
            {
                message = "Request approved, but this player was already in the tracker.";
            }
            else
            {
                try
                {
                    await playerStatsService.AddPlayerAsync(new AddPlayerRequest
                    {
                        Username = joinRequest.RobloxUsername.Trim(),
                        RobloxUserId = joinRequest.RobloxUserId,
                        Club = string.IsNullOrWhiteSpace(joinRequest.Club) || joinRequest.Club.Trim().Equals("None", StringComparison.OrdinalIgnoreCase)
                            ? string.Empty
                            : joinRequest.Club.Trim(),
                        DiscordUserId = string.IsNullOrWhiteSpace(joinRequest.DiscordUserId)
                            ? null
                            : joinRequest.DiscordUserId.Trim()
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return Conflict(new { message = ex.Message });
                }
            }

            // A normal user may register before they are added to the tracker.
            // Once approval creates the player, connect the account that owns
            // this Discord ID to that player. The Discord ID is validated and
            // unique among pending/tracked players, so this does not guess by
            // mutable usernames or create a duplicate player.
            var trackerPlayer = await dbContext.Players
                .FirstOrDefaultAsync(p => p.RobloxUserId == joinRequest.RobloxUserId);
            var websiteUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.DiscordUserId == joinRequest.DiscordUserId);
            if (trackerPlayer is not null && websiteUser is not null && websiteUser.PlayerId is null)
            {
                websiteUser.PlayerId = trackerPlayer.Id;
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new { message });
    }

    /// <summary>
    /// Admin: Delete a join request.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var joinRequest = await dbContext.JoinRequests.FindAsync(id);
        if (joinRequest is null)
        {
            return NotFound();
        }

        dbContext.JoinRequests.Remove(joinRequest);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsDiscordUserId(string value) =>
        value.Length is >= 17 and <= 20 && value.All(char.IsAsciiDigit);
}
