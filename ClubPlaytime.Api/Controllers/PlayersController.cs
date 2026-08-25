using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PlayersController(IPlayerStatsService playerStatsService) : ControllerBase
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
        var player = await playerStatsService.GetPlayerByDiscordUserIdAsync(discordUserId, cancellationToken);
        return player is null ? NotFound() : Ok(player);
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
