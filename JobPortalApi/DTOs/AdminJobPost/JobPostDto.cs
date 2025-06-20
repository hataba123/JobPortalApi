namespace JobPortalApi.DTOs.AdminJobPost
{
    public class JobPostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SkillsRequired { get; set; }
        public string Location { get; set; }
        public decimal Salary { get; set; }
        public Guid EmployerId { get; set; }
        public Guid? CompanyId { get; set; }
        public string Logo { get; set; }
        public string Type { get; set; }
        public List<string> Tags { get; set; }
        public int Applicants { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CategoryId { get; set; }
    }
}
