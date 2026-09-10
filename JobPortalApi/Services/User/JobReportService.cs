using JobPortalApi.DTOs.Report;
using JobPortalApi.Middleware;
using JobPortalApi.Models;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.User;

public sealed class JobReportService : IJobReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IOutboxService _outbox;

    public JobReportService(ApplicationDbContext context, IAuditLogService auditLog, IOutboxService outbox)
    {
        _context = context;
        _auditLog = auditLog;
        _outbox = outbox;
    }

    public async Task<JobReportDto> CreateAsync(Guid actorId, CreateJobReportRequest request)
    {
        var job = await _context.JobPosts.Include(item => item.Company)
            .FirstOrDefaultAsync(item => item.Id == request.JobPostId);
        if (job == null) throw new KeyNotFoundException("Tin tuyển dụng không tồn tại.");
        var report = new JobReport
        {
            Id = Guid.NewGuid(),
            JobPostId = job.Id,
            ReporterId = actorId,
            Reason = request.Reason.Trim(),
            Description = request.Description?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _context.JobReports.Add(report);
        _auditLog.Add("JobReport.Created", "JobReport", report.Id.ToString("D"), null,
            new { report.JobPostId, report.ReporterId, report.Reason });
        _outbox.Add("job-report.created", new { ReportId = report.Id, JobPostId = report.JobPostId, job.Title },
            $"job-report:{report.Id:D}:created");
        await _context.SaveChangesAsync();
        return await GetByIdProjectionAsync(report.Id) ?? throw new InvalidOperationException("Không đọc lại được báo cáo.");
    }

    public async Task<IReadOnlyList<JobReportDto>> GetAllAsync(string? status = null)
    {
        var query = Query();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        var reports = await query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id).ToListAsync();
        return reports.Select(ToDto).ToList();
    }

    public async Task<PagedResultDto<JobReportDto>> GetAllAsync(PagedQuery request, string? status = null)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = Query();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(item => item.Reason.Contains(search) || item.JobPost.Title.Contains(search));
        }
        var total = await query.CountAsync();
        var reports = await query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResultDto<JobReportDto> { Items = reports.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<JobReportDto?> UpdateStatusAsync(Guid id, Guid actorId, string status, byte[]? expectedVersion = null)
    {
        if (!new[] { "Pending", "Resolved", "Dismissed" }.Contains(status, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Trạng thái báo cáo không hợp lệ.");
        var report = await _context.JobReports.Include(item => item.JobPost).ThenInclude(job => job.Company)
            .Include(item => item.Reporter).FirstOrDefaultAsync(item => item.Id == id);
        if (report == null) return null;
        if (expectedVersion is { Length: > 0 })
            _context.Entry(report).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        var before = report.Status;
        report.Status = char.ToUpperInvariant(status[0]) + status[1..].ToLowerInvariant();
        report.ResolvedAt = report.Status == "Pending" ? null : DateTime.UtcNow;
        report.ResolvedBy = report.Status == "Pending" ? null : actorId;
        _auditLog.Add("JobReport.StatusChanged", "JobReport", report.Id.ToString("D"),
            new { Status = before }, new { Status = report.Status });
        _outbox.Add("job-report.status.changed", new { ReportId = report.Id, report.Status },
            $"job-report:{report.Id:D}:status:{report.Status}");
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiConflictException("Báo cáo đã được người khác cập nhật.");
        }
        return ToDto(report);
    }

    private IQueryable<JobReport> Query() => _context.JobReports
        .Include(item => item.JobPost).ThenInclude(job => job.Company)
        .Include(item => item.Reporter)
        .AsNoTracking();

    private async Task<JobReportDto?> GetByIdProjectionAsync(Guid id) =>
        (await Query().FirstOrDefaultAsync(item => item.Id == id)) is { } report ? ToDto(report) : null;

    private static JobReportDto ToDto(JobReport report) => new()
    {
        Id = report.Id,
        JobPostId = report.JobPostId,
        JobTitle = report.JobPost.Title,
        CompanyName = report.JobPost.Company?.Name ?? string.Empty,
        ReporterId = report.ReporterId,
        ReporterEmail = report.Reporter.Email,
        Reason = report.Reason,
        Description = report.Description,
        Status = report.Status,
        CreatedAt = report.CreatedAt,
        ResolvedAt = report.ResolvedAt,
        Version = ConcurrencyToken.Encode(report.RowVersion)
    };
}
