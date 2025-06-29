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
        public string? Education { get; set; }
    }
}
