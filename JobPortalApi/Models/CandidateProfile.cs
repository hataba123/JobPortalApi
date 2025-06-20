using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models
{
    [Table("CandidateProfiles")]
    public class CandidateProfile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [MaxLength(300)]
        public string ResumeUrl { get; set; }

        [MaxLength(1000)]
        public string Experience { get; set; }

        [MaxLength(500)]
        public string Skills { get; set; } // "skill1,skill2"

        [MaxLength(200)]
        public string Education { get; set; }

        public DateTime? Dob { get; set; }

        [MaxLength(10)]
        public string Gender { get; set; }

        [MaxLength(300)]
        public string PortfolioUrl { get; set; }

        [MaxLength(300)]
        public string LinkedinUrl { get; set; }

        [MaxLength(300)]
        public string GithubUrl { get; set; }

        [MaxLength(1000)]
        public string Certificates { get; set; } // "cert1,cert2"

        [MaxLength(1000)]
        public string Summary { get; set; }
    }
}