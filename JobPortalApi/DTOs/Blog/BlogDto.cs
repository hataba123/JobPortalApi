using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Blog
{
    public class BlogDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Category { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public DateTime PublishedAt { get; set; }
        public string ReadTime { get; set; } = string.Empty;
        public int Views { get; set; }
        public int Likes { get; set; }
        public bool Featured { get; set; }
        public string Image { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public BlogAuthorDto Author { get; set; } = null!;
    }
}
