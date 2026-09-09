namespace JobPortalApi.DTOs.JobPost
{
    public class UpdateJobPostDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SkillsRequired { get; set; }
        public string Location { get; set; }
        public decimal Salary { get; set; }
        public string Type { get; set; }
        public string Logo { get; set; }
        public List<string> Tags { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? CompanyName { get; set; }
        public JobPortalApi.Models.Enums.JobPostStatus? Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MinExperienceYears { get; set; }
        public string? EducationRequirement { get; set; }
    }
}
