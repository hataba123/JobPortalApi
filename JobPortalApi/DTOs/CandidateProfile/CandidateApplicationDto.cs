using JobPortalApi.Models.Enums;

namespace JobPortalApi.DTOs.CandidateProfile
{
    public class CandidateApplicationDto
    {
        public Guid JobId { get; set; }
        public Guid JobPostId { get; set; }
        public string JobTitle { get; set; }
        public DateTime AppliedAt { get; set; }
        public string CVUrl { get; set; }
        public ApplyStatus Status { get; set; }
    }
}
