using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models
{
    [Table("MatchResults")]
    public class MatchResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CandidateId { get; set; }
        [ForeignKey(nameof(CandidateId))]
        public User Candidate { get; set; }

        [Required]
        public Guid JobPostId { get; set; }
        [ForeignKey(nameof(JobPostId))]
        public JobPost JobPost { get; set; }

        [Range(0, 100)]
        public int TotalScore { get; set; }

        [Required]
        public string BreakdownJson { get; set; }

        [Required]
        public string MatchedSkillsJson { get; set; }

        [Required]
        public string MissingSkillsJson { get; set; }

        [Required]
        public string ReasonsJson { get; set; }

        [Required, MaxLength(20)]
        public string AlgorithmVersion { get; set; }

        [Required, MaxLength(64)]
        public string InputFingerprint { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
