using System.Data;
using JobPortalApi.DTOs.JobPost;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Interface.User;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Payments;
using JobPortalApi.Middleware;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class JobService : IJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly CreditLedgerService _creditLedgerService;
        private readonly IOutboxService _outbox;

        public JobService(ApplicationDbContext context, CreditLedgerService creditLedgerService, IOutboxService outbox)
        {
            _context = context;
            _creditLedgerService = creditLedgerService;
            _outbox = outbox;
        }

        public async Task<PagedResponse<JobPostDto>> GetAllAsync(JobPostQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = ActivePosts(_context.JobPosts);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(job => job.Title.Contains(search) ||
                    job.Description.Contains(search) || job.SkillsRequired.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(request.Location))
                query = query.Where(job => job.Location == request.Location.Trim());
            if (!string.IsNullOrWhiteSpace(request.Type))
                query = query.Where(job => job.Type == request.Type.Trim());
            if (request.CategoryId.HasValue)
                query = query.Where(job => job.CategoryId == request.CategoryId.Value);
            if (request.MinSalary.HasValue)
                query = query.Where(job => job.Salary >= request.MinSalary.Value);
            var total = await query.CountAsync();
            var entities = await query
                .AsNoTracking()
                .Include(j => j.Category)
                .Include(j => j.Company)
                .OrderByDescending(j => j.CreatedAt)
                .ThenBy(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = entities.Select(ToDto).ToList();
            return new PagedResponse<JobPostDto>
            {
                Items = items,
                Total = total,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<JobPostDto?> GetByIdAsync(Guid id)
        {
            var entity = await ActivePosts(_context.JobPosts.Where(j => j.Id == id))
                .Include(j => j.Category)
                .Include(j => j.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return entity == null ? null : ToDto(entity);
        }

        public async Task<IEnumerable<JobPostDto>> GetByCompanyIdAsync(Guid companyId)
        {
            var entities = await ActivePosts(_context.JobPosts.Where(j => j.CompanyId == companyId))
                .Include(j => j.Category)
                .Include(j => j.Company)
                .AsNoTracking()
                .OrderByDescending(j => j.CreatedAt)
                .ThenBy(j => j.Id)
                .ToListAsync();
            return entities.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<JobPostDto>> GetByCategoryIdAsync(Guid categoryId)
        {
            var entities = await ActivePosts(_context.JobPosts.Where(j => j.CategoryId == categoryId))
                .Include(j => j.Category)
                .Include(j => j.Company)
                .AsNoTracking()
                .OrderByDescending(j => j.CreatedAt)
                .ThenBy(j => j.Id)
                .ToListAsync();
            return entities.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<JobPostDto>> GetByEmployerIdAsync(Guid employerId)
        {
            var entities = await _context.JobPosts.Where(j => j.EmployerId == employerId)
                .Include(j => j.Category)
                .Include(j => j.Company)
                .AsNoTracking()
                .OrderByDescending(j => j.CreatedAt)
                .ThenBy(j => j.Id)
                .ToListAsync();
            return entities.Select(ToDto).ToList();
        }

        public async Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId)
        {
            var requestedStatus = dto.Status ?? JobPostStatus.PendingApproval;
            if (requestedStatus != JobPostStatus.Draft &&
                requestedStatus != JobPostStatus.PendingApproval)
                throw new ArgumentException("Recruiter chỉ được tạo tin ở trạng thái Draft hoặc PendingApproval.");

            var jobId = Guid.NewGuid();
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var job = new JobPost
                {
                    Id = jobId,
                    Title = dto.Title,
                    Description = dto.Description,
                    SkillsRequired = dto.SkillsRequired,
                    Location = dto.Location,
                    Salary = dto.Salary,
                    Type = dto.Type,
                    Logo = dto.Logo,
                    Tags = dto.Tags ?? new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = dto.ExpiresAt,
                    Status = requestedStatus,
                    MinExperienceYears = dto.MinExperienceYears,
                    EducationRequirement = dto.EducationRequirement,
                    CategoryId = dto.CategoryId,
                    CompanyId = dto.CompanyId,
                    EmployerId = employerId
                };

                var consumed = await _creditLedgerService.TryConsumeAsync(
                    employerId,
                    CreditType.JobPost,
                    quantity: 1,
                    idempotencyKey: $"jobpost:create:{job.Id}");
                if (!consumed)
                    throw new InvalidOperationException("Bạn không còn tín dụng đăng tin.");

                _context.JobPosts.Add(job);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });

            return await GetForManagementAsync(jobId)
                ?? throw new InvalidOperationException("Không thể đọc lại tin tuyển dụng vừa tạo.");
        }

        public async Task<JobPostDto?> UpdateAsync(Guid id, UpdateJobPostDto dto, Guid employerId, byte[]? expectedVersion = null)
        {
            if (dto.Status.HasValue && dto.Status is not (JobPostStatus.Draft or JobPostStatus.PendingApproval))
                throw new ArgumentException("Recruiter chỉ được giữ tin ở trạng thái Draft hoặc PendingApproval; Admin phải duyệt để phát hành.");

            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);
            if (job == null) return null;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(job).Property(post => post.RowVersion).OriginalValue = expectedVersion;

            job.Title = dto.Title;
            job.Description = dto.Description;
            job.SkillsRequired = dto.SkillsRequired;
            job.Location = dto.Location;
            job.Salary = dto.Salary;
            job.Type = dto.Type;
            job.Logo = dto.Logo;
            job.Tags = dto.Tags ?? new List<string>();
            job.CategoryId = dto.CategoryId;
            job.CompanyId = dto.CompanyId;
            job.ExpiresAt = dto.ExpiresAt;
            if (dto.MinExperienceYears.HasValue)
                job.MinExperienceYears = dto.MinExperienceYears.Value;
            if (dto.EducationRequirement != null)
                job.EducationRequirement = dto.EducationRequirement;
            if (dto.Status.HasValue)
                job.Status = dto.Status.Value;

            if (job.Status == JobPostStatus.Active)
            {
                _outbox.Add(
                    "matching.job.refresh",
                    new { JobPostId = job.Id },
                    $"matching:job:{job.Id:D}:changed:{Guid.NewGuid():D}");
            }

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Tin tuyển dụng đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return await GetForManagementAsync(job.Id);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid employerId, byte[]? expectedVersion = null)
        {
            var post = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);
            if (post == null) return false;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(post).Property(item => item.RowVersion).OriginalValue = expectedVersion;

            post.DeletedAt = DateTime.UtcNow;
            post.Status = JobPostStatus.Closed;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Tin tuyển dụng đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return true;
        }

        private async Task<JobPostDto?> GetForManagementAsync(Guid id)
        {
            var entity = await _context.JobPosts
                .Include(j => j.Category)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);
            return entity == null ? null : ToDto(entity);
        }

        private static IQueryable<JobPost> ActivePosts(IQueryable<JobPost> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(j => j.Status == JobPostStatus.Active &&
                (!j.ExpiresAt.HasValue || j.ExpiresAt > now));
        }

        private static JobPostDto ToDto(JobPost j)
        {
            return new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                SkillsRequired = j.SkillsRequired,
                Location = j.Location,
                Salary = j.Salary,
                Type = j.Type,
                Logo = j.Logo,
                Tags = j.Tags,
                CreatedAt = j.CreatedAt,
                ExpiresAt = j.ExpiresAt,
                Status = j.Status,
                MinExperienceYears = j.MinExperienceYears,
                EducationRequirement = j.EducationRequirement,
                CategoryName = j.Category != null ? j.Category.Name : "",
                CompanyName = j.Company != null ? j.Company.Name : "",
                Version = ConcurrencyToken.Encode(j.RowVersion)
            };
        }
    }
}
