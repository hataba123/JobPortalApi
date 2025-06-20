namespace JobPortalApi.DTOs.AdminDashboard
{
    public class DashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalJobPosts { get; set; }
        public int TotalReviews { get; set; }
        public int PendingReviews { get; set; }
        public int TotalApplications { get; set; }
        public int ApplicationsToday { get; set; }
        public int NewUsersToday { get; set; }
    }
}
