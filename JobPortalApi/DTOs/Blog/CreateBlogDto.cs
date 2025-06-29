using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Blog
{
    public class CreateBlogDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Excerpt { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Slug { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        public string[] Tags { get; set; } = Array.Empty<string>();

        [StringLength(20)]
        public string ReadTime { get; set; } = string.Empty;

        public bool Featured { get; set; } = false;

        [StringLength(500)]
        public string Image { get; set; } = string.Empty;

        public int AuthorId { get; set; }
    }
}
