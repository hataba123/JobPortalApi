namespace JobPortalApi.DTOs.Blog
{
    public class BlogSearchDto
    {
        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? Sort { get; set; } // "newest", "popular", "trending"
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
    }
}
