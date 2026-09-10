using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PlayersController(
    IPlayerStatsService playerStatsService,
    ClubPlaytimeDbContext dbContext,
    ILogger<PlayersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlayerDto>>> GetPlayers(CancellationToken cancellationToken)
    {
        var players = await playerStatsService.GetPlayersAsync(cancellationToken);
        return Ok(players);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerDetailsDto>> GetPlayer(int id, CancellationToken cancellationToken)
    {
        var player = await playerStatsService.GetPlayerDetailsAsync(id, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlayerDto>> AddPlayer(AddPlayerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var player = await playerStatsService.AddPlayerAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePlayer(int id, CancellationToken cancellationToken)
    {
        var deleted = await playerStatsService.DeletePlayerAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/adjust-playtime")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlayerDetailsDto>> AdjustPlaytime(
        int id,
        AdjustPlaytimeRequest request,
        CancellationToken cancellationToken)
    {
        var player = await playerStatsService.AdjustPlaytimeAsync(id, request, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }

    [HttpGet("by-discord/{discordUserId}")]
    public async Task<ActionResult<PlayerDetailsDto>> GetPlayerByDiscord(string discordUserId, CancellationToken cancellationToken)
    {
        var normalizedDiscordUserId = (discordUserId ?? string.Empty).Trim();
        if (normalizedDiscordUserId.Length == 0)
        {
            return BadRequest(new { message = "A Discord user ID is required." });
        }

        logger.LogInformation("Discord player lookup requested for Discord user ID {DiscordUserId}", normalizedDiscordUserId);
        var player = await playerStatsService.GetPlayerByDiscordUserIdAsync(normalizedDiscordUserId, cancellationToken);
        logger.LogInformation(
            "Discord player lookup for Discord user ID {DiscordUserId} {LookupResult}",
            normalizedDiscordUserId,
            player is null ? "did not find a linked tracker player" : $"resolved tracker player {player.Id}");
        return player is null ? NotFound() : Ok(player);
    }

    /// <summary>
    /// Public: searchable list of existing tracker players for the account-claim
    /// flow. Returns only non-sensitive info (username, avatar, club, claim state).
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PlayerSearchResultDto>>> SearchPlayers(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Existing tracker-player search requested (query supplied: {HasQuery})", !string.IsNullOrWhiteSpace(q));
        var query = dbContext.Players.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLowerInvariant();
            query = query.Where(p => p.Username.ToLower().Contains(needle));
        }

        var claimedPlayerIds = await dbContext.Users
            .Where(u => u.PlayerId != null)
            .Select(u => u.PlayerId!.Value)
            .ToListAsync(cancellationToken);

        var results = await query
            .OrderBy(p => p.Username)
            .Take(50)
            .Select(p => new PlayerSearchResultDto
            {
                PlayerId = p.Id,
                Username = p.Username,
                RobloxUserId = p.RobloxUserId,
                AvatarUrl = p.AvatarUrl,
                Club = p.Club
            })
            .ToListAsync(cancellationToken);

        foreach (var result in results)
        {
            result.IsClaimed = claimedPlayerIds.Contains(result.PlayerId);
        }

        logger.LogInformation("Existing tracker-player search returned {ResultCount} players", results.Count);
        return Ok(results);
    }

    [HttpPost("link-discord")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlayerDetailsDto>> LinkDiscordUser(
        LinkDiscordRequest request,
        CancellationToken cancellationToken)
    {
        var player = await playerStatsService.LinkDiscordUserAsync(request.RobloxUsername, request.DiscordUserId, cancellationToken);
        return player is null ? NotFound(new { message = $"Player with username '{request.RobloxUsername}' not found." }) : Ok(player);
    }

    [HttpPut("{id:int}/club")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlayerDetailsDto>> UpdateClub(
        int id,
        UpdateClubRequest request,
        CancellationToken cancellationToken)
    {
        var player = await playerStatsService.UpdateClubAsync(id, request.Club, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }
}
