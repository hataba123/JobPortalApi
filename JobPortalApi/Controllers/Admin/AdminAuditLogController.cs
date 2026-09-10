using JobPortalApi.DTOs.Audit;
using JobPortalApi.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class AdminAuditLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminAuditLogController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedQuery query, [FromQuery] string? action, [FromQuery] string? entityType)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var logs = _context.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action)) logs = logs.Where(log => log.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType)) logs = logs.Where(log => log.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            logs = logs.Where(log => log.EntityId.Contains(search) || log.Action.Contains(search));
        }
        var total = await logs.CountAsync();
        var items = await logs.OrderByDescending(log => log.CreatedAt).ThenBy(log => log.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(log => new AuditLogDto
            {
                Id = log.Id, ActorId = log.ActorId, Action = log.Action, EntityType = log.EntityType,
                EntityId = log.EntityId, Before = log.Before, After = log.After,
                IpAddress = log.IpAddress, CorrelationId = log.CorrelationId, CreatedAt = log.CreatedAt
            }).ToListAsync();
        return Ok(new PagedResultDto<AuditLogDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }
}
