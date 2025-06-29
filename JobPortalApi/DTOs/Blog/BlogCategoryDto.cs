namespace JobPortalApi.DTOs.Blog
{
    public class BlogCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Count { get; set; }
    }
}
