namespace JobPortalApi.DTOs.Employer { 
public class UpdateJobPostRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public decimal Salary { get; set; }
    public string SkillsRequired { get; set; } = "";
}
}
