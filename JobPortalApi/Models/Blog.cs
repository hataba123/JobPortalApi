using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models
{


        public class Blog
        {
            [Key]
            public int Id { get; set; }

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

            public string Tags { get; set; } = string.Empty; // JSON array as string

            [Required]
            public DateTime PublishedAt { get; set; }

            [StringLength(20)]
            public string ReadTime { get; set; } = string.Empty;

            public int Views { get; set; } = 0;

            public int Likes { get; set; } = 0;

            public bool Featured { get; set; } = false;

            [StringLength(500)]
            public string Image { get; set; } = string.Empty;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            // Foreign Key
            public int AuthorId { get; set; }

            // Navigation Property
            public virtual BlogAuthor Author { get; set; } = null!;

            // Helper method to get tags as array
            public string[] GetTagsArray()
            {
                if (string.IsNullOrEmpty(Tags))
                    return Array.Empty<string>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<string[]>(Tags) ?? Array.Empty<string>();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }

            // Helper method to set tags from array
            public void SetTagsArray(string[] tags)
            {
                Tags = System.Text.Json.JsonSerializer.Serialize(tags);
            }
        }

        public class BlogAuthor
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [StringLength(500)]
            public string Avatar { get; set; } = string.Empty;

            [StringLength(100)]
            public string Role { get; set; } = string.Empty;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // Navigation Property
            public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
        }

        public class BlogCategory
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [StringLength(50)]
            public string Name { get; set; } = string.Empty;

            [StringLength(200)]
            public string? Description { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public class BlogView
        {
            [Key]
            public int Id { get; set; }

            public int BlogId { get; set; }

            public string? UserId { get; set; }

            public string? IpAddress { get; set; }

            public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

            // Navigation Property
            public virtual Blog Blog { get; set; } = null!;
        }

        public class BlogLike
        {
            [Key]
            public int Id { get; set; }

            public int BlogId { get; set; }

            [Required]
            public string UserId { get; set; } = string.Empty;

            public DateTime LikedAt { get; set; } = DateTime.UtcNow;

            // Navigation Property
            public virtual Blog Blog { get; set; } = null!;
        }
    }

