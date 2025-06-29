namespace JobPortalApi.DTOs.CandidateProfile
{
    public class CandidateSearchRequest
    {
        public string? Keyword { get; set; }
        public string? Skill { get; set; }
        public string? Education { get; set; }
        public int? MinYearsExperience { get; set; }
    }
}
