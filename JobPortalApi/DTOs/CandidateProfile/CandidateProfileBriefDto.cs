using System;

namespace JobPortalApi.DTOs.CandidateProfileDto
{
    public class CandidateProfileBriefDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Education { get; set; }
        public string? PreferredLocation { get; set; }
        public string? PreferredJobType { get; set; }
        public decimal? ExpectedSalary { get; set; }
    }
}
