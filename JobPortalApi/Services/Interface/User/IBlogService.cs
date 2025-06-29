using JobPortalApi.DTOs.Blog;
using JobPortalApi.Services.User;

namespace JobPortalApi.Services.Interface.User
{
    public interface IBlogService
    {
        Task<BlogResponseDto> GetBlogsAsync(BlogSearchDto searchDto);
        Task<List<BlogDto>> GetFeaturedBlogsAsync();
        Task<BlogDto?> GetBlogByIdAsync(int id);
        Task<BlogDto?> GetBlogBySlugAsync(string slug);
        Task<BlogDto> CreateBlogAsync(CreateBlogDto createDto);
        Task<BlogDto?> UpdateBlogAsync(int id, UpdateBlogDto updateDto);
        Task<bool> DeleteBlogAsync(int id);
        Task<List<BlogCategoryDto>> GetCategoriesAsync();
        Task<List<string>> GetPopularTagsAsync();
        Task IncrementViewsAsync(int id, string? userId, string? ipAddress);
        Task<BlogLikeResponseDto> ToggleLikeAsync(int id, string userId);
        Task<BlogStatsDto> GetStatsAsync();
        Task<List<BlogAuthorDto>> GetFeaturedAuthorsAsync();
    }
    public class BlogLikeResponseDto
    {
        public int Likes { get; set; }
        public bool IsLiked { get; set; }
    }
}
