namespace JobPortalApi.DTOs.RecruiterDashboard
{
    public class CandidateApplyDto
    {
        public Guid CandidateId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string JobTitle { get; set; }
        public DateTime AppliedAt { get; set; }

    }
}
