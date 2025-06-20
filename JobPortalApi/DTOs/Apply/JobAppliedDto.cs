namespace JobPortalApi.DTOs.Apply
{
    public class JobAppliedDto
    {
        public Guid JobPostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SkillsRequired { get; set; }
        public string Location { get; set; }
        public decimal Salary { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}
