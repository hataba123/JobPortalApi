using JobPortalApi.DTOs.AdminJobPost;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Admin
{
    public class JobPostService : IJobPostService
    {
        private readonly ApplicationDbContext _context;
        public JobPostService(ApplicationDbContext context) => _context = context;

        public async Task<List<JobPostDto>> GetAllJobPostsAsync()
        {
            return await _context.JobPosts
                .AsNoTracking()
                .Select(j => new JobPostDto
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
                    CategoryId = j.CategoryId
                })
                .ToListAsync();
        }

        public async Task<JobPostDto?> GetJobPostByIdAsync(Guid id)
        {
            var j = await _context.JobPosts.FindAsync(id);
            if (j == null) return null;
            return new JobPostDto
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
                CategoryId = j.CategoryId
            };
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
                CreatedAt = dto.CreatedAt,
                CategoryId = dto.CategoryId
            };
            _context.JobPosts.Add(j);
            await _context.SaveChangesAsync();
            return await GetJobPostByIdAsync(j.Id)!;
        }

        public async Task<bool> UpdateJobPostAsync(Guid id, UpdateJobPostDto dto)
        {
            var j = await _context.JobPosts.FindAsync(id);
            if (j == null) return false;
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
            if (dto.CategoryId.HasValue) j.CategoryId = dto.CategoryId.Value;
            _context.JobPosts.Update(j);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteJobPostAsync(Guid id)
        {
            var j = await _context.JobPosts.FindAsync(id);
            if (j == null) return false;
            _context.JobPosts.Remove(j);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
