namespace JobPortalApi.DTOs.Blog
{
    public class BlogResponseDto
    {
        public List<BlogDto> Blogs { get; set; } = new List<BlogDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
    }
}
