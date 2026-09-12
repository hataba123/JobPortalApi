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
    public async Task<IActionResult> GetOutboxSummary(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = _context.OutboxMessages.Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null);
        var oldest = await pending.OrderBy(message => message.OccurredAt).Select(message => (DateTime?)message.OccurredAt).FirstOrDefaultAsync(cancellationToken);
        var deadLettered = await _context.OutboxMessages.CountAsync(message => message.DeadLetteredAt != null, cancellationToken);
        var retried = await _context.OutboxMessages.CountAsync(message => message.Attempts > 1, cancellationToken);
        return Ok(new
        {
            pending = await pending.CountAsync(cancellationToken),
            oldestPendingAt = oldest,
            oldestPendingAgeSeconds = oldest.HasValue ? (now - oldest.Value).TotalSeconds : 0,
            retried,
            deadLettered
        });
    }

    [HttpGet("outbox/dead-lettered")]
    public async Task<IActionResult> GetDeadLetteredOutbox(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.OutboxMessages
            .AsNoTracking()
            .Where(message => message.DeadLetteredAt != null)
            .OrderByDescending(message => message.DeadLetteredAt)
            .ThenBy(message => message.Id);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(message => new
            {
                message.Id,
                message.Type,
                message.OccurredAt,
                message.Attempts,
                message.LastError,
                message.DeadLetteredAt,
                message.CorrelationId
            })
            .ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPost("outbox/{id:guid}/retry")]
    public async Task<IActionResult> RetryDeadLetteredOutbox(Guid id, CancellationToken cancellationToken)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (message == null) return NotFound();
        if (message.DeadLetteredAt == null)
            return Conflict(new { message = "Outbox chưa ở trạng thái lỗi cuối." });

        message.DeadLetteredAt = null;
        message.ProcessedAt = null;
        message.Attempts = 0;
        message.NextAttemptAt = DateTime.UtcNow;
        message.LastError = null;
        message.LockedBy = null;
        message.LockExpiresAt = null;
        await _context.SaveChangesAsync(cancellationToken);
        return Accepted(new { id = message.Id, status = "queued" });
    }
}
