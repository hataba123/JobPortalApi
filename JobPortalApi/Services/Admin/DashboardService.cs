using JobPortalApi.DTOs.AdminDashboard;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Admin
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var totalUsers = await _context.Users.CountAsync();
            var newUsersToday = await _context.Users
                .CountAsync(u => EF.Property<DateTime>(u, "CreatedAt") >= today);

            var totalCompanies = await _context.Companies.CountAsync();
            var totalJobPosts = await _context.JobPosts.CountAsync();
            var totalReviews = await _context.Review.CountAsync();
            var pendingReviews = await _context.Review
                .CountAsync(r => EF.Property<string>(r, "Status") == "Pending");
            var totalApplications = await _context.Jobs.CountAsync();
            var applicationsToday = await _context.Jobs
                .CountAsync(a => a.AppliedAt >= today);

            return new DashboardDto
            {
                TotalUsers = totalUsers,
                NewUsersToday = newUsersToday,
                TotalCompanies = totalCompanies,
                TotalJobPosts = totalJobPosts,
                TotalReviews = totalReviews,
                PendingReviews = pendingReviews,
                TotalApplications = totalApplications,
                ApplicationsToday = applicationsToday
            };
        }
    }
}
