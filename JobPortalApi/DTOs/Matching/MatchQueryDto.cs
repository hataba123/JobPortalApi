using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Matching
{
    public class MatchQueryDto
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [Range(0, 100)]
        public int MinScore { get; set; }
    }

    public class MatchBreakdownDto
    {
        public int Skills { get; set; }
        public int Experience { get; set; }
        public int Education { get; set; }
        public int Preferences { get; set; }
    }

    public class MatchResultDto
    {
        public Guid JobPostId { get; set; }
        public Guid? CandidateId { get; set; }
        public int TotalScore { get; set; }
        public string AlgorithmVersion { get; set; }
        public MatchBreakdownDto Breakdown { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public string Reason { get; set; }
        public string? InputFingerprint { get; set; }
        public MatchJobSummaryDto? JobPost { get; set; }
        public MatchCandidateSummaryDto? Candidate { get; set; }
    }

    public class MatchJobSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Location { get; set; }
        public decimal Salary { get; set; }
        public string? Type { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class MatchCandidateSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class PagedMatchesDto
    {
        public List<MatchResultDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}
