using ClubPlaytime.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlaytime.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MonitorController(IPlayerMonitorRunner monitorRunner) : ControllerBase
{
    [HttpPost("check-now")]
    public async Task<ActionResult<MonitorRunResult>> CheckNow(CancellationToken cancellationToken)
    {
        var result = await monitorRunner.CheckAllPlayersAsync(cancellationToken);
        return Ok(result);
    }
}
