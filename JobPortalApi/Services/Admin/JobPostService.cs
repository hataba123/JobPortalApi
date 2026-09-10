using JobPortalApi.DTOs.AdminJobPost;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Middleware;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Admin
{
    public class JobPostService : IJobPostService
    {
        private readonly ApplicationDbContext _context;
        public JobPostService(ApplicationDbContext context) => _context = context;

        public async Task<List<JobPostDto>> GetAllJobPostsAsync()
        {
            var entities = await _context.JobPosts
                .AsNoTracking()
                .Include(j => j.Category)
                .Include(j => j.Company)
                .ToListAsync();
            return entities.Select(ToDto).ToList();
        }

        public async Task<PagedResultDto<JobPostDto>> GetAllJobPostsAsync(PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.JobPosts.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(job => job.Title.Contains(search) || job.Description.Contains(search));
            }
            var total = await query.CountAsync();
            var ordered = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
                ? request.SortBy?.ToLowerInvariant() switch
                {
                    "title" => query.OrderBy(job => job.Title),
                    "createdat" => query.OrderBy(job => job.CreatedAt),
                    "expiresat" => query.OrderBy(job => job.ExpiresAt),
                    _ => query.OrderBy(job => job.Id)
                }
                : request.SortBy?.ToLowerInvariant() switch
                {
                    "title" => query.OrderByDescending(job => job.Title),
                    "createdat" => query.OrderByDescending(job => job.CreatedAt),
                    "expiresat" => query.OrderByDescending(job => job.ExpiresAt),
                    _ => query.OrderByDescending(job => job.Id)
                };
            var entities = await ordered.ThenBy(job => job.Id).Skip((page - 1) * pageSize)
                .Take(pageSize).Include(job => job.Category).Include(job => job.Company).ToListAsync();
            return new PagedResultDto<JobPostDto>
            {
                Items = entities.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize
            };
        }

        public async Task<JobPostDto?> GetJobPostByIdAsync(Guid id)
        {
            var j = await _context.JobPosts
                .Include(item => item.Category)
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == id);
            if (j == null) return null;
            return ToDto(j);
        }

        public async Task<JobPostDto> CreateJobPostAsync(CreateJobPostDto dto)
        {
            var j = new JobPost
            {
                Title = dto.Title,
                Description = dto.Description,
                SkillsRequired = dto.SkillsRequired,
                Location = dto.Location,
                Salary = dto.Salary,
                EmployerId = dto.EmployerId,
                CompanyId = dto.CompanyId,
                Logo = dto.Logo,
                Type = dto.Type,
                Tags = dto.Tags,
                Applicants = dto.Applicants,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                ExpiresAt = dto.ExpiresAt,
                Status = dto.Status ?? JobPostStatus.Active,
                CategoryId = dto.CategoryId
            };
            _context.JobPosts.Add(j);
            await _context.SaveChangesAsync();
            return await GetJobPostByIdAsync(j.Id)!;
        }

        public async Task<bool> UpdateJobPostAsync(Guid id, UpdateJobPostDto dto, byte[]? expectedVersion = null)
        {
            var j = await _context.JobPosts.FindAsync(id);
            if (j == null) return false;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(j).Property(post => post.RowVersion).OriginalValue = expectedVersion;
            if (!string.IsNullOrWhiteSpace(dto.Title)) j.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Description)) j.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.SkillsRequired)) j.SkillsRequired = dto.SkillsRequired;
            if (!string.IsNullOrWhiteSpace(dto.Location)) j.Location = dto.Location;
            if (dto.Salary.HasValue) j.Salary = dto.Salary.Value;
            if (dto.EmployerId.HasValue) j.EmployerId = dto.EmployerId.Value;
            if (dto.CompanyId.HasValue) j.CompanyId = dto.CompanyId;
            if (!string.IsNullOrWhiteSpace(dto.Logo)) j.Logo = dto.Logo;
            if (!string.IsNullOrWhiteSpace(dto.Type)) j.Type = dto.Type;
            if (dto.Tags != null) j.Tags = dto.Tags;
            if (dto.Applicants.HasValue) j.Applicants = dto.Applicants.Value;
            if (dto.CreatedAt.HasValue) j.CreatedAt = dto.CreatedAt.Value;
            if (dto.ExpiresAt.HasValue) j.ExpiresAt = dto.ExpiresAt.Value;
            if (dto.Status.HasValue) j.Status = dto.Status.Value;
            if (dto.CategoryId.HasValue) j.CategoryId = dto.CategoryId.Value;
            _context.JobPosts.Update(j);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Tin tuyển dụng đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return true;
        }

        public async Task<bool> DeleteJobPostAsync(Guid id, byte[]? expectedVersion = null)
        {
            var j = await _context.JobPosts.FindAsync(id);
            if (j == null) return false;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(j).Property(post => post.RowVersion).OriginalValue = expectedVersion;
            j.DeletedAt = DateTime.UtcNow;
            j.Status = JobPostStatus.Closed;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Tin tuyển dụng đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return true;
        }

        private static JobPostDto ToDto(JobPost j) => new()
        {
            Id = j.Id,
            Title = j.Title,
            Description = j.Description,
            SkillsRequired = j.SkillsRequired,
            Location = j.Location,
            Salary = j.Salary,
            EmployerId = j.EmployerId,
            CompanyId = j.CompanyId,
            Logo = j.Logo,
            Type = j.Type,
            Tags = j.Tags,
            Applicants = j.Applicants,
            CreatedAt = j.CreatedAt,
            ExpiresAt = j.ExpiresAt,
            Status = j.Status,
            MinExperienceYears = j.MinExperienceYears,
            EducationRequirement = j.EducationRequirement,
            CategoryId = j.CategoryId,
            Version = ConcurrencyToken.Encode(j.RowVersion)
        };
    }
}
