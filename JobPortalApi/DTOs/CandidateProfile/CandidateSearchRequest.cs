namespace JobPortalApi.DTOs.CandidateProfile
{
    public class CandidateSearchRequest
    {
        public string? Keyword { get; set; }
        public string? Skill { get; set; }
        public string? Education { get; set; }
        public int? MinYearsExperience { get; set; }
        public int? ExperienceFrom { get; set; }
        public int? ExperienceTo { get; set; }
        public string? Location { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public string? SortDir { get; set; }
    }
}
