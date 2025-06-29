using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Blog
{
    public class CreateBlogAuthorDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Avatar { get; set; } = string.Empty;

        [StringLength(100)]
        public string Role { get; set; } = string.Empty;
    }
}
