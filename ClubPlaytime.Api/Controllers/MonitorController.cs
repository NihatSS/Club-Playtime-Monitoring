using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class MonitorController(IPlayerMonitorRunner monitorRunner) : ControllerBase
{
    [HttpPost("check-now")]
    public async Task<ActionResult<MonitorRunResult>> CheckNow(CancellationToken cancellationToken)
    {
        // A manual check is the "something looks wrong, look now" button, so it
        // bypasses the in-memory roster cache instead of waiting for its TTL.
        var result = await monitorRunner.CheckAllPlayersAsync(cancellationToken, forceRosterRefresh: true);
        return Ok(result);
    }
}
