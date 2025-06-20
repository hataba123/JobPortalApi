using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.Models
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(300)]
        public string Logo { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Location { get; set; }

        [MaxLength(50)]
        public string Employees { get; set; }

        [MaxLength(100)]
        public string Industry { get; set; }

        public int OpenJobs { get; set; }

        public double Rating { get; set; }

        [MaxLength(200)]
        public string Website { get; set; }

        [MaxLength(10)]
        public string Founded { get; set; }

        [MaxLength(500)]
        public string Tags { get; set; } // Lưu dạng "tag1,tag2,tag3"
    }
}
