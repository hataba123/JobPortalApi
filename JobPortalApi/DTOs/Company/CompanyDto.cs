namespace JobPortalApi.DTOs.Company
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Logo { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Employees { get; set; }
        public string? Industry { get; set; }
        public int OpenJobs { get; set; }
        public double Rating { get; set; }
        public string? Website { get; set; }
        public string? Founded { get; set; }
        public string? Tags { get; set; }
    }
}
