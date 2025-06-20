using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Icon { get; set; }

        [MaxLength(50)]
        public string Color { get; set; }
    }
}