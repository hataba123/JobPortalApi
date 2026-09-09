namespace JobPortalApi.DTOs.JobPost
{
    public class JobPostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SkillsRequired { get; set; }
        public string Location { get; set; }
        public decimal Salary { get; set; }
        public string Type { get; set; }
        public string Logo { get; set; }
        public List<string> Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public JobPortalApi.Models.Enums.JobPostStatus Status { get; set; }
        public string CategoryName { get; set; }
        public string CompanyName { get; set; }
    }
}
