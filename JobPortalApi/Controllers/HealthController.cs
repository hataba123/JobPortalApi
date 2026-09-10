using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context) => _context = context;

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Liveness() => Ok(new
    {
        status = "ok",
        timestamp = DateTimeOffset.UtcNow
    });

    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Readiness(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Database.CanConnectAsync(cancellationToken))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unavailable",
                    timestamp = DateTimeOffset.UtcNow
                });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unavailable",
                timestamp = DateTimeOffset.UtcNow
            });
        }

        return Ok(new
        {
            status = "ready",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
