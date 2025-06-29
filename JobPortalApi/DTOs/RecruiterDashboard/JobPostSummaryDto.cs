namespace JobPortalApi.DTOs.RecruiterDashboard
{
    public class JobPostSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Applicants { get; set; }
    }
}
