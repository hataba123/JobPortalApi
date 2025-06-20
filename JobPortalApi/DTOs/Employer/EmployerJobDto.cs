namespace JobPortalApi.DTOs.Employer
{
    public class EmployerJobDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal Salary { get; set; }
        public string SkillsRequired { get; set; } = "";
    }
}
