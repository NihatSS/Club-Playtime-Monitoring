using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfileController(
    ClubPlaytimeDbContext dbContext,
    IPlayerStatsService playerStatsService,
    Services.PlayerProgressService progressService) : ControllerBase
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
            DiscordUserId = user.DiscordUserId,
            RobloxUsername = user.Player?.Username ?? user.RobloxUsername,
            RobloxUserId = user.Player?.RobloxUserId ?? user.RobloxUserId,
            BannerUrl = user.BannerUrl,
            BannerImageVersion = user.BannerImage is { Length: > 0 } banner
                ? BannerImageVersion(banner)
                : null
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
                    ProfileUrl = details.ProfileUrl,
                    LastSeenOnSite = details.LastSeenOnSite
                };

                (response.WeeklyLeaderboardPosition, response.TotalLeaderboardPosition) =
                    await ComputeLeaderboardPositionsAsync(user.PlayerId.Value, cancellationToken);

                // Streaks, achievements count and all-time rank for the profile
                // summary. Additive: never breaks the profile if it fails.
                try
                {
                    response.Progress = await progressService.GetSummaryAsync(user.PlayerId.Value, cancellationToken);
                }
                catch
                {
                    // Progress summary is optional.
                }
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
    /// Set or clear the custom banner image on the caller's own profile hero.
    /// Only HTTPS image URLs are accepted (stored as-is; rendered by the client);
    /// an empty value resets to the default gradient. Clears any uploaded image.
    /// </summary>
    [HttpPost("banner")]
    public async Task<IActionResult> UpdateBanner(UpdateBannerRequest request, CancellationToken cancellationToken)
    {
        var currentUsername = User.Identity?.Name;
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == currentUsername, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var bannerUrl = (request.BannerUrl ?? string.Empty).Trim();

        if (bannerUrl.Length == 0)
        {
            user.BannerUrl = null;
            user.BannerImage = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Banner reset to default.", bannerUrl = (string?)null });
        }

        if (!Uri.TryCreate(bannerUrl, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
        {
            return BadRequest(new { message = "Banner must be an HTTPS image URL." });
        }

        if (bannerUrl.Length > 700)
        {
            return BadRequest(new { message = "Banner URL must be 700 characters or fewer." });
        }
        user.BannerUrl = bannerUrl;
        user.BannerImage = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Banner updated.", bannerUrl });
    }

    /// <summary>
    /// Upload a banner image file from the user's PC (multipart form field
    /// "file"). Stored in the database and served via the banner-image
    /// endpoint. The client downscales the picture before uploading; a hard
    /// 2 MB limit backstops that. Replaces any banner URL.
    /// </summary>
    [HttpPost("banner/upload")]
    [RequestSizeLimit(2_500_000)]
    public async Task<IActionResult> UploadBanner(IFormFile file, CancellationToken cancellationToken)
    {
        var currentUsername = User.Identity?.Name;
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == currentUsername, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No image file was provided." });
        }

        if (file.Length > 2_000_000)
        {
            return BadRequest(new { message = "Image is too large. Please choose an image under 2 MB." });
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "The selected file is not an image." });
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        // Reject anything that is not one of the formats the browsers render
        // for <img> (png / jpeg / gif / webp magic numbers).
        if (!ImageSignature.IsSupportedImage(bytes))
        {
            return BadRequest(new { message = "Unsupported image format. Use PNG, JPEG, GIF or WebP." });
        }

        user.BannerImage = bytes;
        user.BannerUrl = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Banner image uploaded.",
            bannerImageVersion = BannerImageVersion(bytes),
            contentType
        });
    }

    /// <summary>
    /// The signed-in user's uploaded banner image (used to bust the cache right
    /// after an upload without a full profile reload).
    /// </summary>
    [HttpGet("banner-image/me")]
    public async Task<IActionResult> GetMyBannerImage(CancellationToken cancellationToken)
    {
        var currentUsername = User.Identity?.Name;
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == currentUsername, cancellationToken);

        if (user?.BannerImage is not { Length: > 0 } bytes)
        {
            return NotFound();
        }

        return BannerFileResult(bytes);
    }

    /// <summary>
    /// Uploaded banner image for a user id. Public so profile visitors can see
    /// it without additional client wiring; id-only access leaks nothing.
    /// </summary>
    [HttpGet("banner-image/{userId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBannerImage(int userId, CancellationToken cancellationToken)
    {
        var bytes = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.BannerImage)
            .FirstOrDefaultAsync(cancellationToken);

        if (bytes is not { Length: > 0 })
        {
            return NotFound();
        }

        return BannerFileResult(bytes);
    }

    private static FileContentResult BannerFileResult(byte[] bytes)
    {
        return new FileContentResult(bytes, ImageSignature.SniffContentType(bytes))
        {
            // Cache for a day, revalidated via the ?v= hash the client appends.
            EnableRangeProcessing = false
        };
    }

    /// <summary>Stable content-hash used as the ?v= cache-buster for banner images.</summary>
    internal static string BannerImageVersion(byte[] bytes)
    {
        var hash = 17L;
        hash = hash * 31 + bytes.Length;
        hash = hash * 31 + bytes[0];
        hash = hash * 31 + bytes[bytes.Length / 2];
        hash = hash * 31 + bytes[^1];
        return Math.Abs(hash).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Magic-number checks for the image formats browsers render in <img>.</summary>
    private static class ImageSignature
    {
        public static bool IsSupportedImage(byte[] b) => SniffContentType(b) != "application/octet-stream";

        public static string SniffContentType(byte[] b)
        {
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            {
                return "image/png";
            }
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            {
                return "image/jpeg";
            }
            if (b.Length >= 6 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46)
            {
                return "image/gif";
            }
            if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
                b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            {
                return "image/webp";
            }
            return "application/octet-stream";
        }
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

    /// <summary>
    /// Save the game info the user fills in on their own profile: Roblox
    /// username, Roblox user ID and Discord ID. This is what their join
    /// request will use (Phase 10), and what admins see when reviewing.
    /// </summary>
    [HttpPost("game-info")]
    public async Task<IActionResult> UpdateGameInfo(UpdateGameInfoRequest request, CancellationToken cancellationToken)
    {
        var currentUsername = User.Identity?.Name;
        var user = await dbContext.Users
            .Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Username == currentUsername, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var robloxUsername = (request.RobloxUsername ?? string.Empty).Trim();
        var discordUserId = (request.DiscordUserId ?? string.Empty).Trim();

        if (robloxUsername.Length > 100)
        {
            return BadRequest(new { message = "Roblox username must be 100 characters or fewer." });
        }

        if (discordUserId.Length > 0 && !IsDiscordUserId(discordUserId))
        {
            return BadRequest(new { message = "Discord User ID must contain 17 to 20 digits. Enable Discord Developer Mode and use Copy User ID." });
        }

        var robloxUserId = request.RobloxUserId;
        if (robloxUserId is not null && robloxUserId <= 0)
        {
            return BadRequest(new { message = "Roblox user ID must be a positive number." });
        }

        // Keep the dedicated Discord link endpoint semantics: validate uniqueness.
        if (discordUserId.Length > 0)
        {
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
        }

        // Users already linked to a tracker player keep the canonical values
        // from that player; only the Discord ID remains editable there.
        if (user.Player is not null)
        {
            user.DiscordUserId = discordUserId.Length == 0 ? null : discordUserId;
            if (user.Player.DiscordUserId != user.DiscordUserId)
            {
                user.Player.DiscordUserId = user.DiscordUserId;
                user.Player.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            user.RobloxUsername = robloxUsername.Length == 0 ? null : robloxUsername;
            user.RobloxUserId = robloxUserId;
            user.DiscordUserId = discordUserId.Length == 0 ? null : discordUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Game info saved." });
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
