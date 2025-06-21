using JobPortalApi.DTOs.SavedJob;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class SavedJobService : ISavedJobService
    {
        private readonly ApplicationDbContext _context;

        public SavedJobService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SavedJobDto>> GetSavedJobsAsync(Guid userId)
        {
            return await _context.SavedJobs
                .Where(s => s.UserId == userId)
                .Include(s => s.JobPost)
                .Select(s => new SavedJobDto
                {
                    Id = s.Id,
                    JobPostId = s.JobPostId,
                    Title = s.JobPost.Title,
                    Location = s.JobPost.Location,
                    SavedAt = s.SavedAt
                })
                .ToListAsync();
        }

        public async Task SaveJobAsync(Guid userId, Guid jobPostId)
        {
            var exists = await _context.SavedJobs.AnyAsync(s => s.UserId == userId && s.JobPostId == jobPostId);
            if (exists) throw new Exception("Bạn đã lưu công việc này rồi.");

            var savedJob = new SavedJob
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                JobPostId = jobPostId,
                SavedAt = DateTime.UtcNow
            };

            _context.SavedJobs.Add(savedJob);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UnsaveJobAsync(Guid userId, Guid jobPostId)
        {
            var job = await _context.SavedJobs
                .FirstOrDefaultAsync(s => s.UserId == userId && s.JobPostId == jobPostId);

            if (job == null) return false;

            _context.SavedJobs.Remove(job);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
