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
            var jobPosts = _context.JobPosts
                .AsNoTracking()
                .Where(j => j.EmployerId == recruiterId);

            var totalJobPosts = await jobPosts.CountAsync();
            var totalApplicants = await _context.Jobs
                .AsNoTracking()
                .CountAsync(a => a.JobPost.EmployerId == recruiterId);

            var recentJobPosts = await jobPosts
                .OrderByDescending(j => j.CreatedAt)
                .ThenBy(j => j.Id)
                .Take(5)
                .Select(j => new JobPostSummaryDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    CreatedAt = j.CreatedAt,
                    Applicants = j.Applicants
                })
                .ToListAsync();

            var recentApplicants = await _context.Jobs
                .AsNoTracking()
                .Where(a => a.JobPost.EmployerId == recruiterId)
                .OrderByDescending(a => a.AppliedAt)
                .ThenBy(a => a.Id)
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
                TotalJobPosts = totalJobPosts,
                TotalApplicants = totalApplicants,
                RecentJobPosts = recentJobPosts,
                RecentApplicants = recentApplicants
            };
        }
    }
}
