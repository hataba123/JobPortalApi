using JobPortalApi.DTOs.Interview;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.Middleware;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JobPortalApi.Services.User;

public sealed class InterviewService : IInterviewService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IOutboxService _outbox;

    public InterviewService(ApplicationDbContext context, IAuditLogService auditLog, IOutboxService outbox)
    {
        _context = context;
        _auditLog = auditLog;
        _outbox = outbox;
    }

    public async Task<InterviewDto> CreateAsync(Guid applicationId, Guid actorId, bool isAdmin, CreateInterviewRequest request, byte[]? applicationVersion)
    {
        // Giữ conflict check và insert trong cùng critical section của SQL Server.
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var application = await _context.Jobs
            .Include(item => item.JobPost)
            .Include(item => item.Candidate)
            .FirstOrDefaultAsync(item => item.Id == applicationId);
        if (application == null) throw new KeyNotFoundException("Không tìm thấy hồ sơ ứng tuyển.");
        if (!isAdmin && application.JobPost.EmployerId != actorId)
            throw new UnauthorizedAccessException("Bạn không có quyền lên lịch cho hồ sơ này.");
        if (application.Status != ApplyStatus.Screening)
            throw new InvalidOperationException("Chỉ có thể lên lịch khi hồ sơ đang ở bước Screening.");

        ValidateRequest(request.Type, request.StartAt, request.EndAt, request.Location, request.MeetingUrl);
        await EnsureNoOverlapAsync(actorId, request.StartAt, request.EndAt, null);
        if (applicationVersion is { Length: > 0 })
            _context.Entry(application).Property(item => item.RowVersion).OriginalValue = applicationVersion;

        var now = DateTime.UtcNow;
        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Type = request.Type,
            StartAt = request.StartAt.ToUniversalTime(),
            EndAt = request.EndAt.ToUniversalTime(),
            Location = request.Location,
            MeetingUrl = request.MeetingUrl,
            InterviewerId = actorId,
            Status = InterviewStatus.Scheduled,
            Result = InterviewResult.Pending,
            Notes = request.Notes,
            CreatedAt = now
        };

        application.Status = ApplyStatus.Interview;
        _context.Interviews.Add(interview);
        _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            FromStatus = ApplyStatus.Screening,
            ToStatus = ApplyStatus.Interview,
            ChangedBy = actorId,
            ChangedAt = now,
            Reason = "Interview scheduled"
        });
        _auditLog.Add("Interview.Created", "Interview", interview.Id.ToString("D"), null,
            new { interview.Id, interview.ApplicationId, interview.Type, interview.StartAt, interview.EndAt });
        _auditLog.Add("Application.StatusChanged", "Application", application.Id.ToString("D"),
            new { Status = ApplyStatus.Screening }, new { Status = ApplyStatus.Interview, InterviewId = interview.Id });
        _outbox.Add("interview.scheduled", new
        {
            InterviewId = interview.Id,
            ApplicationId = application.Id,
            CandidateId = application.CandidateId,
            CandidateEmail = application.Candidate.Email,
            JobTitle = application.JobPost.Title,
            interview.StartAt,
            interview.EndAt,
            interview.MeetingUrl,
            interview.Location
        }, $"interview:{interview.Id:D}:scheduled");

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiConflictException("Hồ sơ đã được người khác cập nhật. Vui lòng tải lại dữ liệu.");
        }
        return ToDto(interview);
        });
    }

    public async Task<InterviewDto?> GetByIdAsync(Guid id, Guid actorId, bool isAdmin, bool isCandidate)
    {
        var interview = await Query().FirstOrDefaultAsync(item => item.Id == id);
        if (interview == null) return null;
        if (!isAdmin && (isCandidate
                ? interview.Application.CandidateId != actorId
                : interview.Application.JobPost.EmployerId != actorId))
            return null;
        return ToDto(interview);
    }

    public async Task<IReadOnlyList<InterviewDto>> GetAsync(Guid actorId, bool isAdmin, bool isCandidate, Guid? applicationId = null, string? status = null)
    {
        var query = Query();
        if (!isAdmin)
            query = isCandidate
                ? query.Where(item => item.Application.CandidateId == actorId)
                : query.Where(item => item.Application.JobPost.EmployerId == actorId);
        if (applicationId.HasValue) query = query.Where(item => item.ApplicationId == applicationId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InterviewStatus>(status, true, out var parsed))
            query = query.Where(item => item.Status == parsed);
        var interviews = await query.OrderBy(item => item.StartAt).ThenBy(item => item.Id)
            .ToListAsync();
        return interviews.Select(ToDto).ToList();
    }

    public async Task<PagedResultDto<InterviewDto>> GetPagedAsync(
        Guid actorId,
        bool isAdmin,
        bool isCandidate,
        PagedQuery query,
        Guid? applicationId = null,
        string? status = null)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = Query();
        if (!isAdmin)
        {
            source = isCandidate
                ? source.Where(item => item.Application.CandidateId == actorId)
                : source.Where(item => item.Application.JobPost.EmployerId == actorId);
        }
        if (applicationId.HasValue) source = source.Where(item => item.ApplicationId == applicationId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InterviewStatus>(status, true, out var parsed))
            source = source.Where(item => item.Status == parsed);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(item => item.Application.JobPost.Title.Contains(search) || item.Application.Candidate.FullName.Contains(search));
        }
        var total = await source.CountAsync();
        var items = await source.OrderBy(item => item.StartAt).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResultDto<InterviewDto>
        {
            Items = items.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<InterviewDto?> UpdateAsync(Guid id, Guid actorId, bool isAdmin, UpdateInterviewRequest request, byte[]? expectedVersion)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var interview = await Query().FirstOrDefaultAsync(item => item.Id == id);
        if (interview == null) return null;
        if (!CanManage(interview, actorId, isAdmin)) return null;
        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Chỉ có thể cập nhật lịch đang Scheduled.");
        if (expectedVersion is { Length: > 0 })
            _context.Entry(interview).Property(item => item.RowVersion).OriginalValue = expectedVersion;

        var type = request.Type ?? interview.Type;
        var start = request.StartAt?.ToUniversalTime() ?? interview.StartAt;
        var end = request.EndAt?.ToUniversalTime() ?? interview.EndAt;
        var location = request.Location ?? interview.Location;
        var meetingUrl = request.MeetingUrl ?? interview.MeetingUrl;
        ValidateRequest(type, start, end, location, meetingUrl);
        await EnsureNoOverlapAsync(interview.InterviewerId, start, end, interview.Id);
        var before = new { interview.Type, interview.StartAt, interview.EndAt, interview.Location, interview.MeetingUrl, interview.Notes };
        interview.Type = type;
        interview.StartAt = start;
        interview.EndAt = end;
        interview.Location = location;
        interview.MeetingUrl = meetingUrl;
        interview.Notes = request.Notes ?? interview.Notes;
        interview.UpdatedAt = DateTime.UtcNow;
        _auditLog.Add("Interview.Updated", "Interview", interview.Id.ToString("D"), before,
            new { interview.Type, interview.StartAt, interview.EndAt, interview.Location, interview.MeetingUrl, interview.Notes });
        _outbox.Add("interview.rescheduled", new
        {
            InterviewId = interview.Id,
            ApplicationId = interview.ApplicationId,
            CandidateId = interview.Application.CandidateId,
            CandidateEmail = interview.Application.Candidate.Email,
            JobTitle = interview.Application.JobPost.Title,
            interview.StartAt,
            interview.EndAt,
            interview.MeetingUrl,
            interview.Location
        },
            $"interview:{interview.Id:D}:updated:{interview.StartAt.Ticks}");
        await SaveWithConcurrencyAsync();
        await transaction.CommitAsync();
        return ToDto(interview);
        });
    }

    public async Task<InterviewDto?> CompleteAsync(Guid id, Guid actorId, bool isAdmin, CompleteInterviewRequest request, byte[]? expectedInterviewVersion, byte[]? expectedApplicationVersion)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var interview = await Query().FirstOrDefaultAsync(item => item.Id == id);
        if (interview == null) return null;
        if (!CanManage(interview, actorId, isAdmin)) return null;
        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Chỉ có thể hoàn tất lịch đang Scheduled.");
        if (expectedInterviewVersion is { Length: > 0 })
            _context.Entry(interview).Property(item => item.RowVersion).OriginalValue = expectedInterviewVersion;
        if (expectedApplicationVersion is { Length: > 0 })
            _context.Entry(interview.Application).Property(item => item.RowVersion).OriginalValue = expectedApplicationVersion;

        interview.Status = InterviewStatus.Completed;
        interview.Result = request.Result;
        interview.Notes = request.Notes ?? interview.Notes;
        interview.UpdatedAt = DateTime.UtcNow;
        var targetStatus = request.Result == InterviewResult.Passed ? ApplyStatus.Offer : ApplyStatus.Rejected;
        var oldStatus = interview.Application.Status;
        interview.Application.Status = targetStatus;
        _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = interview.ApplicationId,
            FromStatus = oldStatus,
            ToStatus = targetStatus,
            ChangedBy = actorId,
            ChangedAt = DateTime.UtcNow,
            Reason = $"Interview result: {request.Result}"
        });
        _auditLog.Add("Interview.Completed", "Interview", interview.Id.ToString("D"),
            new { Status = InterviewStatus.Scheduled, Result = InterviewResult.Pending },
            new { interview.Status, interview.Result });
        _auditLog.Add("Application.StatusChanged", "Application", interview.ApplicationId.ToString("D"),
            new { Status = oldStatus }, new { Status = targetStatus, Reason = request.Result.ToString() });
        _outbox.Add("application.status.changed", new
        {
            ApplicationId = interview.ApplicationId,
            CandidateId = interview.Application.CandidateId,
            CandidateEmail = interview.Application.Candidate.Email,
            JobTitle = interview.Application.JobPost.Title,
            FromStatus = oldStatus,
            ToStatus = targetStatus
        }, $"application:{interview.ApplicationId:D}:status:{targetStatus}");
        await SaveWithConcurrencyAsync();
        await transaction.CommitAsync();
        return ToDto(interview);
        });
    }

    public async Task<InterviewDto?> CancelAsync(Guid id, Guid actorId, bool isAdmin, byte[]? expectedVersion)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var interview = await Query().FirstOrDefaultAsync(item => item.Id == id);
        if (interview == null) return null;
        if (!CanManage(interview, actorId, isAdmin)) return null;
        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Chỉ có thể hủy lịch đang Scheduled.");
        if (expectedVersion is { Length: > 0 })
            _context.Entry(interview).Property(item => item.RowVersion).OriginalValue = expectedVersion;
        interview.Status = InterviewStatus.Cancelled;
        interview.UpdatedAt = DateTime.UtcNow;
        _auditLog.Add("Interview.Cancelled", "Interview", interview.Id.ToString("D"),
            new { Status = InterviewStatus.Scheduled }, new { Status = InterviewStatus.Cancelled });
        if (interview.Application.Status == ApplyStatus.Interview)
        {
            interview.Application.Status = ApplyStatus.Screening;
            _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                ApplicationId = interview.ApplicationId,
                FromStatus = ApplyStatus.Interview,
                ToStatus = ApplyStatus.Screening,
                ChangedBy = actorId,
                ChangedAt = DateTime.UtcNow,
                Reason = "Interview cancelled; application returned to Screening"
            });
            _auditLog.Add("Application.StatusChanged", "Application", interview.ApplicationId.ToString("D"),
                new { Status = ApplyStatus.Interview },
                new { Status = ApplyStatus.Screening, Reason = "Interview cancelled" });
            _outbox.Add("application.status.changed", new
            {
                ApplicationId = interview.ApplicationId,
                CandidateId = interview.Application.CandidateId,
                CandidateEmail = interview.Application.Candidate.Email,
                JobTitle = interview.Application.JobPost.Title,
                FromStatus = ApplyStatus.Interview,
                ToStatus = ApplyStatus.Screening
            }, $"application:{interview.ApplicationId:D}:status:Screening:interview-cancelled:{interview.Id:D}");
        }
        _outbox.Add("interview.cancelled", new
        {
            InterviewId = interview.Id,
            ApplicationId = interview.ApplicationId,
            CandidateId = interview.Application.CandidateId,
            CandidateEmail = interview.Application.Candidate.Email,
            JobTitle = interview.Application.JobPost.Title
        },
            $"interview:{interview.Id:D}:cancelled");
        await SaveWithConcurrencyAsync();
        await transaction.CommitAsync();
        return ToDto(interview);
        });
    }

    private IQueryable<Interview> Query() => _context.Interviews
        .Include(item => item.Application)
            .ThenInclude(application => application.JobPost)
        .Include(item => item.Application)
            .ThenInclude(application => application.Candidate)
        .AsTracking();

    private bool CanManage(Interview interview, Guid actorId, bool isAdmin) =>
        isAdmin || interview.Application.JobPost.EmployerId == actorId;

    private async Task EnsureNoOverlapAsync(Guid interviewerId, DateTime start, DateTime end, Guid? ignoredId)
    {
        var overlap = await _context.Interviews.AnyAsync(item =>
            item.InterviewerId == interviewerId &&
            item.Status == InterviewStatus.Scheduled &&
            (!ignoredId.HasValue || item.Id != ignoredId.Value) &&
            item.StartAt < end && start < item.EndAt);
        if (overlap) throw new InvalidOperationException("Interviewer đã có lịch trùng trong khoảng thời gian này.");
    }

    private static void ValidateRequest(InterviewType type, DateTime start, DateTime end, string? location, string? meetingUrl)
    {
        if (start <= DateTime.UtcNow) throw new ArgumentException("Thời gian phỏng vấn phải ở tương lai.");
        if (end <= start) throw new ArgumentException("EndAt phải lớn hơn StartAt.");
        if (type == InterviewType.Online)
        {
            if (!Uri.TryCreate(meetingUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Phỏng vấn online cần MeetingUrl HTTPS hợp lệ.");
        }
        if (type == InterviewType.Onsite && string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Phỏng vấn onsite cần Location.");
        if (type == InterviewType.Phone && string.IsNullOrWhiteSpace(location) && string.IsNullOrWhiteSpace(meetingUrl))
            throw new ArgumentException("Phỏng vấn phone cần thông tin liên hệ.");
    }

    private async Task SaveWithConcurrencyAsync()
    {
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiConflictException(
                "Lịch phỏng vấn hoặc hồ sơ đã được người khác cập nhật. Vui lòng tải lại dữ liệu.",
                "CONCURRENCY_CONFLICT");
        }
    }

    private static InterviewDto ToDto(Interview item) => new()
    {
        Id = item.Id,
        ApplicationId = item.ApplicationId,
        JobPostId = item.Application.JobPostId,
        CandidateId = item.Application.CandidateId,
        CandidateName = item.Application.Candidate.FullName,
        JobTitle = item.Application.JobPost.Title,
        Type = item.Type,
        StartAt = item.StartAt,
        EndAt = item.EndAt,
        Location = item.Location,
        MeetingUrl = item.MeetingUrl,
        InterviewerId = item.InterviewerId,
        Status = item.Status,
        Result = item.Result,
        Notes = item.Notes,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        Version = ConcurrencyToken.Encode(item.RowVersion),
        ApplicationVersion = ConcurrencyToken.Encode(item.Application.RowVersion)
    };
}
