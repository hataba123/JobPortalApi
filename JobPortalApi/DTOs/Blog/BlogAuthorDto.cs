using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Blog
{
    public class BlogAuthorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
