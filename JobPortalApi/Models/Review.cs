using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.Models
{
        [Table("Reviews")]
        public class Review
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            public Guid UserId { get; set; }
            [ForeignKey(nameof(UserId))]
            public User User { get; set; }

            [Required]
            public Guid CompanyId { get; set; }
            [ForeignKey(nameof(CompanyId))]
            public Company Company { get; set; }

            [Required]
            public int Rating { get; set; }

            [MaxLength(1000)]
            public string Comment { get; set; }

            [Required]
            public DateTime CreatedAt { get; set; }
        }
    }