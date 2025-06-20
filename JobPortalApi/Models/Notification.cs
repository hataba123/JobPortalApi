using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.Models
{
        [Table("Notifications")]
        public class Notification
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            public Guid UserId { get; set; }
            [ForeignKey(nameof(UserId))]
            public User User { get; set; }

            [Required]
            public string Message { get; set; }

            [Required]
            public bool Read { get; set; }

            [Required]
            public DateTime CreatedAt { get; set; }

            [MaxLength(50)]
            public string Type { get; set; }
        }
    }

