namespace JobPortalApi.DTOs.RecruiterDashboard
{
    public class RecruiterDashboardDto
    {
        public int TotalJobPosts { get; set; }
        public int TotalApplicants { get; set; }
        public List<JobPostSummaryDto> RecentJobPosts { get; set; }
        public List<CandidateApplyDto> RecentApplicants { get; set; }
    }
}
