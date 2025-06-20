using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.AdminJobPost
{
    public class CreateJobPostDto
    {
        [Required, MaxLength(200)] public string Title { get; set; }
        [Required] public string Description { get; set; }
        [MaxLength(500)] public string SkillsRequired { get; set; }
        [MaxLength(100)] public string Location { get; set; }
        [Range(0, double.MaxValue)] public decimal Salary { get; set; }
        [Required] public Guid EmployerId { get; set; }
        public Guid? CompanyId { get; set; }
        [MaxLength(300)] public string Logo { get; set; }
        [MaxLength(50)] public string Type { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Applicants { get; set; }
        [Required] public DateTime CreatedAt { get; set; }
        [Required] public Guid CategoryId { get; set; }
    }
}
