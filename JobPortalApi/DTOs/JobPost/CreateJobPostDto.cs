namespace JobPortalApi.DTOs.JobPost
{
    public class CreateJobPostDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public decimal Salary { get; set; }
        public string Type { get; set; }  // Full-time, part-time
        public string Logo { get; set; }
        public List<string> Tags { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? CompanyId { get; set; }
    }

}
