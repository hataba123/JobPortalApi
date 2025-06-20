using JobPortalApi.DTOs.Employer;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class EmployerService : IEmployerService
    {
        private readonly ApplicationDbContext _context;

        public EmployerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployerJobDto>> GetMyPostedJobsAsync(Guid employerId)
        {
            return await _context.JobPosts
                .Where(j => j.EmployerId == employerId)
                .Select(j => new EmployerJobDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Location = j.Location,
                    Salary = j.Salary,
                    SkillsRequired = j.SkillsRequired
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateJobAsync(Guid jobId, Guid employerId, UpdateJobPostRequest request)
        {
            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == employerId);
            if (job == null) return false;

            job.Title = request.Title;
            job.Description = request.Description;
            job.Location = request.Location;
            job.Salary = request.Salary;
            job.SkillsRequired = request.SkillsRequired;

            _context.JobPosts.Update(job);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteJobAsync(Guid jobId, Guid employerId)
        {
            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == employerId);
            if (job == null) return false;

            _context.JobPosts.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
