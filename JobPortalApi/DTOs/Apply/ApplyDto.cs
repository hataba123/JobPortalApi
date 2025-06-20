namespace JobPortalApi.DTOs.Apply
{
    public class ApplyDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public string CandidateName { get; set; }
        public Guid JobPostId { get; set; }
        public string JobTitle { get; set; }
        public string CVUrl { get; set; }
        public string Status { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}
