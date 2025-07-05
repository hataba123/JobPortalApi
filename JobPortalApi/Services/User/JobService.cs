using JobPortalApi.DTOs.JobPost;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using System;
using AutoMapper;

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
            return await _context.JobPosts
                .Include(j => j.Category)
                .Include(j => j.Company)
                .Select(j => new JobPostDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Location = j.Location,
                    Salary = j.Salary,
                    Type = j.Type,
                    Logo = j.Logo,
                    Tags = j.Tags,
                    CreatedAt = j.CreatedAt,
                    CategoryName = j.Category.Name,
                    CompanyName = j.Company != null ? j.Company.Name : ""
                })
                .ToListAsync();
        }

        public async Task<JobPostDto?> GetByIdAsync(Guid id)
        {
            return await _context.JobPosts
                .Include(j => j.Category)
                .Include(j => j.Company)
                .Where(j => j.Id == id)
                .Select(j => new JobPostDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Location = j.Location,
                    Salary = j.Salary,
                    Type = j.Type,
                    Logo = j.Logo,
                    Tags = j.Tags,
                    CreatedAt = j.CreatedAt,
                    CategoryName = j.Category.Name,
                    CompanyName = j.Company != null ? j.Company.Name : ""
                })
                .FirstOrDefaultAsync();
        }
        public async Task<IEnumerable<JobPostDto>> GetByCompanyIdAsync(Guid companyId)
        {
            var jobPosts = await _context.JobPosts
                .Where(j => j.CompanyId == companyId)
                .Include(j => j.Category) // ✅ Load Category để tránh null
                .Include(j => j.Company)  // ✅ Load Company nếu cần tên công ty
                .ToListAsync();

            var jobPostDtos = jobPosts.Select(j => new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Location = j.Location,
                Salary = j.Salary,
                Type = j.Type,
                Logo = j.Logo,
                Tags = j.Tags,
                CreatedAt = j.CreatedAt,
                CategoryName = j.Category != null ? j.Category.Name : "", // tránh null
                CompanyName = j.Company != null ? j.Company.Name : ""
            });

            return jobPostDtos;
        }
        public async Task<IEnumerable<JobPostDto>> GetByCategoryIdAsync(Guid categoryId)
        {
            var jobPosts = await _context.JobPosts
                .Where(j => j.CategoryId == categoryId)
                .Select(j => new JobPostDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Location = j.Location,
                    Salary = j.Salary,
                    Type = j.Type,
                    Logo = j.Logo,
                    Tags = j.Tags,
                    CreatedAt = j.CreatedAt,
                    CategoryName = j.Category.Name,
                    CompanyName = j.Company != null ? j.Company.Name : ""
                })
                .ToListAsync();

            return jobPosts;
        }
        public async Task<IEnumerable<JobPostDto>> GetByEmployerIdAsync(Guid employerId)
        {
            return await _context.JobPosts
                .Include(j => j.Category)
                .Include(j => j.Company)
                .Where(j => j.EmployerId == employerId)
                .Select(j => new JobPostDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Location = j.Location,
                    Salary = j.Salary,
                    Type = j.Type,
                    Logo = j.Logo,
                    Tags = j.Tags,
                    CreatedAt = j.CreatedAt,
                    CategoryName = j.Category.Name,
                    CompanyName = j.Company != null ? j.Company.Name : ""
                })
                .ToListAsync();
        }

        public async Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId)
        {
            var job = new JobPost
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                Salary = dto.Salary,
                Type = dto.Type,
                Logo = dto.Logo,
                Tags = dto.Tags,
                CreatedAt = DateTime.UtcNow,
                CategoryId = dto.CategoryId,
                CompanyId = dto.CompanyId,
                EmployerId = employerId
            };
            _context.JobPosts.Add(job);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(job.Id) ?? throw new Exception("Failed to retrieve created job post.");
        }

        public async Task<JobPostDto?> UpdateAsync(Guid id, UpdateJobPostDto dto)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job == null) return null;

            job.Title = dto.Title;
            job.Description = dto.Description;
            job.Location = dto.Location;
            job.Salary = dto.Salary;
            job.Type = dto.Type;
            job.Logo = dto.Logo;
            job.Tags = dto.Tags;
            job.CategoryId = dto.CategoryId;
            job.CompanyId = dto.CompanyId;
            job.CreatedAt = DateTime.UtcNow;

            _context.JobPosts.Update(job);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(job.Id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var post = await _context.JobPosts.FindAsync(id);
            if (post == null) return false;

            _context.JobPosts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
