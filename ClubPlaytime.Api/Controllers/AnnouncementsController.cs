using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnnouncementsController(ClubPlaytimeDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Public: list announcements, newest first. The notification feed merges
    /// these with tournament notifications.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AnnouncementDto>>> List(
        [FromQuery] int limit = 20)
    {
        // Feed caps at 12 items today; keep the API limit modest.
        var safeLimit = Math.Clamp(limit, 1, 50);

        var announcements = await dbContext.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .Take(safeLimit)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Body = a.Body,
                LinkUrl = a.LinkUrl,
                CreatedBy = a.CreatedBy,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(announcements);
    }

    /// <summary>Admin: post an announcement.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AnnouncementDto>> Create(CreateAnnouncementRequest request)
    {
        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            return BadRequest(new { message = "Title is required." });
        }

        var body = (request.Body ?? string.Empty).Trim();
        var linkUrl = (request.LinkUrl ?? string.Empty).Trim();

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            username = dbContext.Users.FirstOrDefault(u => u.Username == "matiaspro")?.Username ?? "admin";
        }

        var announcement = new Announcement
        {
            Title = title.Length > 120 ? title[..120] : title,
            Body = body.Length == 0 ? null : body.Length > 500 ? body[..500] : body,
            // Only in-app hash routes are accepted to avoid open-redirect links.
            LinkUrl = linkUrl.Length == 0 ? null : linkUrl.Length > 300 ? linkUrl[..300] : linkUrl,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Announcements.Add(announcement);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(List),
            new { id = announcement.Id },
            new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Body = announcement.Body,
                LinkUrl = announcement.LinkUrl,
                CreatedBy = announcement.CreatedBy,
                CreatedAt = announcement.CreatedAt
            });
    }

    /// <summary>Admin: delete an announcement.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var announcement = await dbContext.Announcements.FindAsync(id);
        if (announcement is null)
        {
            return NotFound(new { message = "Announcement not found." });
        }

        dbContext.Announcements.Remove(announcement);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
