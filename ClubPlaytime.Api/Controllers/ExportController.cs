using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExportController(IPlayerStatsService playerStatsService) : ControllerBase
{
    [HttpGet("playtime.csv")]
    public async Task<IActionResult> ExportPlaytimeCsv(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var csv = await playerStatsService.ExportCsvAsync(from, to, cancellationToken);
        var fileName = $"club-playtime-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }
}
