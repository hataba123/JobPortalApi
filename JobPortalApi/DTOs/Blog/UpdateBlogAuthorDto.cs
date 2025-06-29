using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Blog
{
    public class UpdateBlogAuthorDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        [StringLength(100)]
        public string? Role { get; set; }
    }
}
