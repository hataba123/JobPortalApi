using JobPortalApi.DTOs.RecruiterDashboard;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class RecruiterDashboardService : IRecruiterDashboardService
    {
        private readonly ApplicationDbContext _context;

        public RecruiterDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RecruiterDashboardDto> GetDashboardAsync(Guid recruiterId)
        {
            var jobPosts = await _context.JobPosts
                .Where(j => j.EmployerId == recruiterId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var totalApplicants = await _context.Jobs
                .Where(a => jobPosts.Select(j => j.Id).Contains(a.JobPostId))
                .CountAsync();

            var recentJobPosts = jobPosts
                .Take(5)
                .Select(j => new JobPostSummaryDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    CreatedAt = j.CreatedAt,
                    Applicants = j.Applicants
                }).ToList();

            var recentApplicants = await _context.Jobs
                .Where(a => jobPosts.Select(j => j.Id).Contains(a.JobPostId))
                .OrderByDescending(a => a.AppliedAt)
                .Take(5)
                .Select(a => new CandidateApplyDto
                {
                    CandidateId = a.CandidateId,
                    FullName = a.Candidate.FullName,
                    Email = a.Candidate.Email,
                    JobTitle = a.JobPost.Title,
                    AppliedAt = a.AppliedAt
                }).ToListAsync();

            return new RecruiterDashboardDto
            {
                TotalJobPosts = jobPosts.Count,
                TotalApplicants = totalApplicants,
                RecentJobPosts = recentJobPosts,
                RecentApplicants = recentApplicants
            };
        }
    }
}
