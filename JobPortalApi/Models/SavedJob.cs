using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.Models
{
        [Table("SavedJobs")]
        public class SavedJob
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            public Guid UserId { get; set; }
            [ForeignKey(nameof(UserId))]
            public User User { get; set; }

            [Required]
            public Guid JobPostId { get; set; }
            [ForeignKey(nameof(JobPostId))]
            public JobPost JobPost { get; set; }

            [Required]
            public DateTime SavedAt { get; set; }
        }
    }
