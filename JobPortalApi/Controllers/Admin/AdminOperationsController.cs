using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Controllers.Admin;

[ApiController]
[Route("api/admin/operations")]
[Authorize(Roles = "Admin")]
public sealed class AdminOperationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminOperationsController(ApplicationDbContext context) => _context = context;

    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutboxSummary()
    {
        var now = DateTime.UtcNow;
        var pending = _context.OutboxMessages.Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null);
        var oldest = await pending.OrderBy(message => message.OccurredAt).Select(message => (DateTime?)message.OccurredAt).FirstOrDefaultAsync();
        var deadLettered = await _context.OutboxMessages.CountAsync(message => message.DeadLetteredAt != null);
        var retried = await _context.OutboxMessages.CountAsync(message => message.Attempts > 1);
        return Ok(new
        {
            pending = await pending.CountAsync(),
            oldestPendingAt = oldest,
            oldestPendingAgeSeconds = oldest.HasValue ? (now - oldest.Value).TotalSeconds : 0,
            retried,
            deadLettered
        });
    }
}
