using JobPortalApi.DTOs.JobPost;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class JobService : IJobService
    {
        private readonly ApplicationDbContext _context;

        public JobService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<JobPostDto>> GetAllAsync()
        {
            return await Project(ActivePosts(_context.JobPosts))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<JobPostDto?> GetByIdAsync(Guid id)
        {
            return await Project(ActivePosts(_context.JobPosts.Where(j => j.Id == id)))
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<JobPostDto>> GetByCompanyIdAsync(Guid companyId)
        {
            return await Project(ActivePosts(_context.JobPosts.Where(j => j.CompanyId == companyId)))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobPostDto>> GetByCategoryIdAsync(Guid categoryId)
        {
            return await Project(ActivePosts(_context.JobPosts.Where(j => j.CategoryId == categoryId)))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobPostDto>> GetByEmployerIdAsync(Guid employerId)
        {
            return await Project(_context.JobPosts.Where(j => j.EmployerId == employerId))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId)
        {
            var job = new JobPost
            {
                Id = Guid.NewGuid(),
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
                Status = dto.Status ?? JobPostStatus.Active,
                MinExperienceYears = dto.MinExperienceYears,
                EducationRequirement = dto.EducationRequirement,
                CategoryId = dto.CategoryId,
                CompanyId = dto.CompanyId,
                EmployerId = employerId
            };

            _context.JobPosts.Add(job);
            await _context.SaveChangesAsync();

            return await GetForManagementAsync(job.Id)
                ?? throw new InvalidOperationException("Không thể đọc lại tin tuyển dụng vừa tạo.");
        }

        public async Task<JobPostDto?> UpdateAsync(Guid id, UpdateJobPostDto dto, Guid employerId)
        {
            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);
            if (job == null) return null;

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

            await _context.SaveChangesAsync();
            return await GetForManagementAsync(job.Id);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid employerId)
        {
            var post = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);
            if (post == null) return false;

            _context.JobPosts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<JobPostDto?> GetForManagementAsync(Guid id)
        {
            return await Project(_context.JobPosts.Where(j => j.Id == id))
                .FirstOrDefaultAsync();
        }

        private static IQueryable<JobPost> ActivePosts(IQueryable<JobPost> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(j => j.Status == JobPostStatus.Active &&
                (!j.ExpiresAt.HasValue || j.ExpiresAt > now));
        }

        private static IQueryable<JobPostDto> Project(IQueryable<JobPost> query)
        {
            return query.Select(j => new JobPostDto
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
                CompanyName = j.Company != null ? j.Company.Name : ""
            });
        }
    }
}
