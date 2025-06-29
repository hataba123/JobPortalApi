using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Blog
{
    public class UpdateBlogDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? Excerpt { get; set; }

        public string? Content { get; set; }

        [StringLength(100)]
        public string? Slug { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public string[]? Tags { get; set; }

        [StringLength(20)]
        public string? ReadTime { get; set; }

        public bool? Featured { get; set; }

        [StringLength(500)]
        public string? Image { get; set; }
    }
}
