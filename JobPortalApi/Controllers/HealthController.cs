using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace JobPortalApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer? _redis;

    public HealthController(ApplicationDbContext context, IServiceProvider services)
    {
        _context = context;
        _redis = services.GetService<IConnectionMultiplexer>();
    }

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
            if (_redis != null)
                await _redis.GetDatabase().PingAsync();
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
