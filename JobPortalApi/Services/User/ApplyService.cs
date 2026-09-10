using JobPortalApi.DTOs.Apply;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Middleware;
using JobPortalApi.Services.Helpers;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.User;

public sealed class ApplyService : IApplyService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IOutboxService _outbox;

    public ApplyService(
        ApplicationDbContext context,
        IAuditLogService auditLog,
        IOutboxService outbox)
    {
        _context = context;
        _auditLog = auditLog;
        _outbox = outbox;
    }

    public async Task ApplyToJobAsync(Guid candidateId, JobApplicationRequest request)
    {
        var jobPost = await _context.JobPosts
            .Include(job => job.Company)
            .FirstOrDefaultAsync(job => job.Id == request.JobPostId);
        if (jobPost == null)
            throw new KeyNotFoundException("Công việc không tồn tại.");
        if (jobPost.Status != JobPostStatus.Active ||
            (jobPost.ExpiresAt.HasValue && jobPost.ExpiresAt.Value <= DateTime.UtcNow))
            throw new InvalidOperationException("Tin tuyển dụng không còn nhận hồ sơ.");

        var candidateProfile = await _context.candidateProfiles
            .FirstOrDefaultAsync(profile => profile.UserId == candidateId);
        if (candidateProfile == null)
            throw new KeyNotFoundException("Hồ sơ ứng viên chưa tồn tại.");

        var cvUrl = candidateProfile.ResumeUrl;
        if (string.IsNullOrEmpty(cvUrl))
            throw new InvalidOperationException("Bạn cần tải lên CV trước khi ứng tuyển.");
        if (PrivateCvStorage.Resolve(cvUrl) is not { } validPath || !File.Exists(validPath))
            throw new InvalidOperationException("File CV không tồn tại, vui lòng tải lên lại.");

        var application = new Job
        {
            Id = Guid.NewGuid(),
            JobPostId = request.JobPostId,
            CandidateId = candidateId,
            CVUrl = cvUrl,
            AppliedAt = DateTime.UtcNow,
            Status = ApplyStatus.Applied
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Jobs.Add(application);
        AddHistory(application, null, ApplyStatus.Applied, candidateId, null);
        _auditLog.Add("Application.Created", "Application", application.Id.ToString("D"), null,
            new { application.Id, application.JobPostId, application.CandidateId, application.Status });
        _outbox.Add("application.created.notification", new
        {
            ApplicationId = application.Id,
            EmployerId = jobPost.EmployerId,
            CandidateId = candidateId,
            CandidateEmail = await _context.Users.Where(user => user.Id == candidateId).Select(user => user.Email).FirstOrDefaultAsync(),
            JobTitle = jobPost.Title
        }, $"application:{application.Id:D}:created");
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            throw new ApiConflictException("Bạn đã ứng tuyển công việc này rồi.");
        }
    }

    public async Task<List<CandidateApplicationDto>> GetCandidatesAppliedToJob(Guid employerId, Guid jobPostId)
    {
        var ownsJob = await _context.JobPosts.AnyAsync(job => job.Id == jobPostId && job.EmployerId == employerId);
        if (!ownsJob)
            throw new UnauthorizedAccessException("Không tìm thấy công việc hoặc bạn không có quyền.");

        return await _context.Jobs
            .AsNoTracking()
            .Where(application => application.JobPostId == jobPostId)
            .Select(application => new CandidateApplicationDto
            {
                Id = application.Id,
                CandidateId = application.CandidateId,
                FullName = application.Candidate.FullName,
                Email = application.Candidate.Email,
                AppliedAt = application.AppliedAt,
                CVUrl = application.CVUrl,
                Status = application.Status,
                Version = ConcurrencyToken.Encode(application.RowVersion)
            })
            .ToListAsync();
    }

    public async Task<PagedResultDto<CandidateApplicationDto>> GetCandidatesAppliedToJobPaged(
        Guid employerId, Guid jobPostId, PagedQuery request)
    {
        var ownsJob = await _context.JobPosts.AnyAsync(job => job.Id == jobPostId && job.EmployerId == employerId);
        if (!ownsJob) throw new UnauthorizedAccessException("Không tìm thấy công việc hoặc bạn không có quyền.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _context.Jobs.AsNoTracking()
            .Where(application => application.JobPostId == jobPostId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(application => application.AppliedAt)
            .ThenBy(application => application.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(application => new CandidateApplicationDto
            {
                Id = application.Id,
                CandidateId = application.CandidateId,
                FullName = application.Candidate.FullName,
                Email = application.Candidate.Email,
                AppliedAt = application.AppliedAt,
                CVUrl = application.CVUrl,
                Status = application.Status,
                Version = ConcurrencyToken.Encode(application.RowVersion)
            })
            .ToListAsync();
        return new PagedResultDto<CandidateApplicationDto>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<List<JobAppliedDto>> GetJobsAppliedByCandidate(Guid candidateId)
    {
        return await _context.Jobs
            .AsNoTracking()
            .Where(application => application.CandidateId == candidateId)
            .Select(application => new JobAppliedDto
            {
                Id = application.Id,
                JobPostId = application.JobPostId,
                Title = application.JobPost.Title,
                Location = application.JobPost.Location,
                Salary = application.JobPost.Salary,
                SkillsRequired = application.JobPost.SkillsRequired,
                Description = application.JobPost.Description,
                AppliedAt = application.AppliedAt,
                Status = application.Status,
                Version = ConcurrencyToken.Encode(application.RowVersion)
            })
            .OrderByDescending(application => application.AppliedAt)
            .ThenBy(application => application.Id)
            .ToListAsync();
    }

    public async Task<PagedResultDto<JobAppliedDto>> GetJobsAppliedByCandidatePaged(
        Guid candidateId, PagedQuery request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _context.Jobs.AsNoTracking().Where(application => application.CandidateId == candidateId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(application => application.AppliedAt)
            .ThenBy(application => application.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(application => new JobAppliedDto
            {
                Id = application.Id,
                JobPostId = application.JobPostId,
                Title = application.JobPost.Title,
                Location = application.JobPost.Location,
                Salary = application.JobPost.Salary,
                SkillsRequired = application.JobPost.SkillsRequired,
                Description = application.JobPost.Description,
                AppliedAt = application.AppliedAt,
                Status = application.Status,
                Version = ConcurrencyToken.Encode(application.RowVersion)
            })
            .ToListAsync();
        return new PagedResultDto<JobAppliedDto>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<List<ApplyDto>> GetAllAsync()
    {
        var applications = await _context.Jobs
            .AsNoTracking()
            .Include(application => application.Candidate)
            .Include(application => application.JobPost)
            .OrderByDescending(application => application.AppliedAt)
            .ThenBy(application => application.Id)
            .ToListAsync();
        return applications.Select(ToDto).ToList();
    }

    public async Task<PagedResultDto<ApplyDto>> GetAllPagedAsync(PagedQuery request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _context.Jobs.AsNoTracking()
            .Include(application => application.Candidate)
            .Include(application => application.JobPost)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(application => application.Candidate.FullName.Contains(search) ||
                application.JobPost.Title.Contains(search));
        }
        var total = await query.CountAsync();
        var ordered = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? request.SortBy?.ToLowerInvariant() switch
            {
                "status" => query.OrderBy(application => application.Status),
                "candidate" => query.OrderBy(application => application.Candidate.FullName),
                _ => query.OrderBy(application => application.AppliedAt)
            }
            : request.SortBy?.ToLowerInvariant() switch
            {
                "status" => query.OrderByDescending(application => application.Status),
                "candidate" => query.OrderByDescending(application => application.Candidate.FullName),
                _ => query.OrderByDescending(application => application.AppliedAt)
            };
        var applications = await ordered.ThenBy(application => application.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResultDto<ApplyDto>
        {
            Items = applications.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<ApplyDto?> GetByIdAsync(Guid id)
    {
        var application = await _context.Jobs
            .AsNoTracking()
            .Where(application => application.Id == id)
            .Include(application => application.Candidate)
            .Include(application => application.JobPost)
            .FirstOrDefaultAsync();
        return application == null ? null : ToDto(application);
    }

    public async Task<ApplyDto?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin, bool isRecruiter = false)
    {
        var query = _context.Jobs.AsNoTracking().Where(application => application.Id == id);
        if (!isAdmin)
        {
            query = isRecruiter
                ? query.Where(application => application.JobPost.EmployerId == userId)
                : query.Where(application => application.CandidateId == userId);
        }

        var application = await query
            .Include(item => item.Candidate)
            .Include(item => item.JobPost)
            .FirstOrDefaultAsync();
        return application == null ? null : ToDto(application);
    }

    public async Task<TransitionResultDto?> UpdateStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        bool isAdmin,
        byte[]? expectedVersion,
        string? reason)
    {
        var application = await _context.Jobs
            .Include(item => item.JobPost)
            .Include(item => item.Candidate)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (application == null) return null;
        if (!isAdmin && application.JobPost.EmployerId != actorId) return null;

        var nextStatus = ParseStatus(status);
        if (!isAdmin && !IsAllowedRecruiterTransition(application.Status, nextStatus))
            throw new InvalidOperationException($"Không thể chuyển hồ sơ từ {application.Status} sang {nextStatus}.");
        if (nextStatus == ApplyStatus.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cần cung cấp lý do từ chối hồ sơ.");

        return await ChangeStatusAsync(application, nextStatus, actorId, expectedVersion, reason,
            isAdmin ? "Application.StatusOverride" : "Application.StatusChanged");
    }

    public async Task<TransitionResultDto?> WithdrawAsync(Guid id, Guid candidateId, byte[]? expectedVersion, string? reason)
    {
        var application = await _context.Jobs
            .Include(item => item.JobPost)
            .Include(item => item.Candidate)
            .FirstOrDefaultAsync(item => item.Id == id && item.CandidateId == candidateId);
        if (application == null) return null;
        if (application.Status is ApplyStatus.Hired or ApplyStatus.Rejected or ApplyStatus.Withdrawn)
            throw new InvalidOperationException("Hồ sơ đã ở trạng thái kết thúc.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cần cung cấp lý do rút hồ sơ.");

        return await ChangeStatusAsync(application, ApplyStatus.Withdrawn, candidateId, expectedVersion, reason,
            "Application.Withdrawn");
    }

    public async Task<TransitionResultDto?> OverrideStatusAsync(
        Guid id,
        ApplyStatus status,
        Guid actorId,
        byte[]? expectedVersion,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Admin override bắt buộc phải có lý do.");
        var application = await _context.Jobs
            .Include(item => item.JobPost)
            .Include(item => item.Candidate)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (application == null) return null;
        return await ChangeStatusAsync(application, status, actorId, expectedVersion, reason,
            "Application.StatusOverride");
    }

    public async Task<List<ApplicationStatusHistoryDto>?> GetHistoryAsync(Guid id, Guid actorId, bool isAdmin)
    {
        var canRead = isAdmin || await _context.Jobs.AnyAsync(application => application.Id == id &&
            (application.CandidateId == actorId || application.JobPost.EmployerId == actorId));
        if (!canRead) return null;

        return await _context.ApplicationStatusHistories
            .AsNoTracking()
            .Where(history => history.ApplicationId == id)
            .OrderBy(history => history.ChangedAt)
            .Select(history => new ApplicationStatusHistoryDto
            {
                Id = history.Id,
                ApplicationId = history.ApplicationId,
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                ChangedBy = history.ChangedBy,
                ChangedAt = history.ChangedAt,
                Reason = history.Reason
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var application = await _context.Jobs.FindAsync(id);
        if (application == null) return false;
        _context.Jobs.Remove(application);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<TransitionResultDto> ChangeStatusAsync(
        Job application,
        ApplyStatus nextStatus,
        Guid actorId,
        byte[]? expectedVersion,
        string? reason,
        string action)
    {
        if (expectedVersion is { Length: > 0 })
            _context.Entry(application).Property(item => item.RowVersion).OriginalValue = expectedVersion;

        var oldStatus = application.Status;
        if (oldStatus == nextStatus)
        {
            if (expectedVersion is { Length: > 0 } &&
                !application.RowVersion.SequenceEqual(expectedVersion))
            {
                throw new ApiConflictException(
                    "Hồ sơ đã được người khác cập nhật. Vui lòng tải lại dữ liệu.",
                    "CONCURRENCY_CONFLICT",
                    new { version = ConcurrencyToken.Encode(application.RowVersion) });
            }
            return ToTransition(application);
        }

        application.Status = nextStatus;
        AddHistory(application, oldStatus, nextStatus, actorId, reason);
        _auditLog.Add(action, "Application", application.Id.ToString("D"),
            new { Status = oldStatus }, new { Status = nextStatus, Reason = reason });
        _outbox.Add("application.status.changed", new
        {
            ApplicationId = application.Id,
            CandidateId = application.CandidateId,
            CandidateEmail = application.Candidate.Email,
            JobTitle = application.JobPost.Title,
            FromStatus = oldStatus,
            ToStatus = nextStatus
        }, $"application:{application.Id:D}:status:{nextStatus}");

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await _context.Entry(application).GetDatabaseValuesAsync();
            var currentVersion = current == null
                ? null
                : ConcurrencyToken.Encode(current[nameof(Job.RowVersion)] as byte[]);
            throw new ApiConflictException(
                "Hồ sơ đã được người khác cập nhật. Vui lòng tải lại dữ liệu.",
                "CONCURRENCY_CONFLICT",
                new { version = currentVersion });
        }

        return ToTransition(application);
    }

    private void AddHistory(Job application, ApplyStatus? from, ApplyStatus to, Guid? actorId, string? reason)
    {
        _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            FromStatus = from,
            ToStatus = to,
            ChangedBy = actorId,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        });
    }

    private static TransitionResultDto ToTransition(Job application) => new()
    {
        Id = application.Id,
        Status = application.Status,
        Version = ConcurrencyToken.Encode(application.RowVersion)
    };

    private static ApplyDto ToDto(Job application) => new()
    {
        Id = application.Id,
        CandidateId = application.CandidateId,
        CandidateName = application.Candidate.FullName,
        JobPostId = application.JobPostId,
        JobTitle = application.JobPost.Title,
        CVUrl = application.CVUrl,
        Status = application.Status.ToString(),
        AppliedAt = application.AppliedAt,
        Version = ConcurrencyToken.Encode(application.RowVersion)
    };

    private static ApplyStatus ParseStatus(string value)
    {
        if (Enum.TryParse<ApplyStatus>(value, true, out var parsed)) return parsed;
        return value.Trim().ToLowerInvariant() switch
        {
            "pending" => ApplyStatus.Applied,
            "reviewed" => ApplyStatus.Screening,
            "accepted" => ApplyStatus.Offer,
            "rejected" => ApplyStatus.Rejected,
            _ => throw new ArgumentException("Trạng thái hồ sơ không hợp lệ.")
        };
    }

    private static bool IsAllowedRecruiterTransition(ApplyStatus from, ApplyStatus to) =>
        (from, to) switch
        {
            (ApplyStatus.Applied, ApplyStatus.Screening) => true,
            (ApplyStatus.Offer, ApplyStatus.Hired) => true,
            (ApplyStatus.Applied or ApplyStatus.Screening or ApplyStatus.Interview or ApplyStatus.Offer, ApplyStatus.Rejected) => true,
            _ => false
        };
}
