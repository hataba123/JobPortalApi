using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models
{
    [Table("JobPosts")]
    public class JobPost
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [MaxLength(500)]
        public string SkillsRequired { get; set; }

        [MaxLength(100)]
        public string Location { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Required]
        public Guid EmployerId { get; set; }
        [ForeignKey(nameof(EmployerId))]
        public User Employer { get; set; }

        public Guid? CompanyId { get; set; }
        [ForeignKey(nameof(CompanyId))]
        public Company Company { get; set; }

        [MaxLength(300)]
        public string Logo { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }

        [MaxLength(500)]
        public List<string> Tags { get; set; } = new List<string>();
        public int Applicants { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public Guid CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }
        public Guid? CompanyName { get; set; }

    }
}