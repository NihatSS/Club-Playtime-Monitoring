using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JoinRequestController(ClubPlaytimeDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Public: Submit a request to be added to the tracker.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<JoinRequestDto>> SubmitRequest(SubmitJoinRequest request)
    {
        // Check if user already has a pending request
        var existingPending = await dbContext.JoinRequests
            .AnyAsync(r => r.RobloxUserId == request.RobloxUserId && r.Status == "Pending");

        if (existingPending)
        {
            return Conflict(new { message = "You already have a pending request." });
        }

        // Check if user is already tracked
        var alreadyTracked = await dbContext.Players
            .AnyAsync(p => p.RobloxUserId == request.RobloxUserId);

        if (alreadyTracked)
        {
            return Conflict(new { message = "This player is already being tracked." });
        }

        var joinRequest = new JoinRequest
        {
            RobloxUsername = request.RobloxUsername,
            RobloxUserId = request.RobloxUserId,
            DiscordUserId = request.DiscordUserId,
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

        await dbContext.SaveChangesAsync();

        return Ok(new { message = $"Request {review.Status.ToLower()}." });
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
}
