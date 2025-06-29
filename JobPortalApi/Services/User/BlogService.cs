using JobPortalApi.DTOs.Blog;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace JobPortalApi.Services.User
{

    public class BlogService : IBlogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BlogService> _logger;

        public BlogService(ApplicationDbContext context, ILogger<BlogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BlogResponseDto> GetBlogsAsync(BlogSearchDto searchDto)
        {
            try
            {
                var query = _context.Blogs
                    .Include(b => b.Author)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchDto.Search))
                {
                    var searchTerm = searchDto.Search.ToLower();
                    query = query.Where(b =>
                        b.Title.ToLower().Contains(searchTerm) ||
                        b.Excerpt.ToLower().Contains(searchTerm) ||
                        b.Content.ToLower().Contains(searchTerm) ||
                        b.Tags.ToLower().Contains(searchTerm)
                    );
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(searchDto.Category) && searchDto.Category != "Tất cả")
                {
                    query = query.Where(b => b.Category == searchDto.Category);
                }

                // Apply sorting
                query = searchDto.Sort?.ToLower() switch
                {
                    "newest" => query.OrderByDescending(b => b.PublishedAt),
                    "popular" => query.OrderByDescending(b => b.Views),
                    "trending" => query.OrderByDescending(b => b.Likes),
                    _ => query.OrderByDescending(b => b.PublishedAt)
                };

                // Get total count before pagination
                var total = await query.CountAsync();

                // Apply pagination
                var skip = (searchDto.Page - 1) * searchDto.Limit;
                var blogs = await query
                    .Skip(skip)
                    .Take(searchDto.Limit)
                    .ToListAsync();

                var blogDtos = blogs.Select(MapToDto).ToList();

                return new BlogResponseDto
                {
                    Blogs = blogDtos,
                    Total = total,
                    Page = searchDto.Page,
                    Limit = searchDto.Limit,
                    TotalPages = (int)Math.Ceiling((double)total / searchDto.Limit)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blogs");
                throw;
            }
        }

        public async Task<List<BlogDto>> GetFeaturedBlogsAsync()
        {
            try
            {
                var blogs = await _context.Blogs
                    .Include(b => b.Author)
                    .Where(b => b.Featured)
                    .OrderByDescending(b => b.PublishedAt)
                    .Take(6)
                    .ToListAsync();

                return blogs.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured blogs");
                throw;
            }
        }

        public async Task<BlogDto?> GetBlogByIdAsync(int id)
        {
            try
            {
                var blog = await _context.Blogs
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Id == id);

                return blog != null ? MapToDto(blog) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog by id: {Id}", id);
                throw;
            }
        }

        public async Task<BlogDto?> GetBlogBySlugAsync(string slug)
        {
            try
            {
                var blog = await _context.Blogs
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Slug == slug);

                return blog != null ? MapToDto(blog) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog by slug: {Slug}", slug);
                throw;
            }
        }

        public async Task<BlogDto> CreateBlogAsync(CreateBlogDto createDto)
        {
            try
            {
                var blog = new Blog
                {
                    Title = createDto.Title,
                    Excerpt = createDto.Excerpt,
                    Content = createDto.Content,
                    Slug = createDto.Slug ?? GenerateSlug(createDto.Title),
                    Category = createDto.Category,
                    ReadTime = createDto.ReadTime,
                    Featured = createDto.Featured,
                    Image = createDto.Image,
                    PublishedAt = DateTime.UtcNow,
                    AuthorId = createDto.AuthorId
                };

                blog.SetTagsArray(createDto.Tags);

                _context.Blogs.Add(blog);
                await _context.SaveChangesAsync();

                // Reload with author
                await _context.Entry(blog).Reference(b => b.Author).LoadAsync();

                return MapToDto(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog");
                throw;
            }
        }

        public async Task<BlogDto?> UpdateBlogAsync(int id, UpdateBlogDto updateDto)
        {
            try
            {
                var blog = await _context.Blogs.FindAsync(id);
                if (blog == null) return null;

                if (!string.IsNullOrEmpty(updateDto.Title))
                    blog.Title = updateDto.Title;

                if (!string.IsNullOrEmpty(updateDto.Excerpt))
                    blog.Excerpt = updateDto.Excerpt;

                if (!string.IsNullOrEmpty(updateDto.Content))
                    blog.Content = updateDto.Content;

                if (!string.IsNullOrEmpty(updateDto.Slug))
                    blog.Slug = updateDto.Slug;

                if (!string.IsNullOrEmpty(updateDto.Category))
                    blog.Category = updateDto.Category;

                if (!string.IsNullOrEmpty(updateDto.ReadTime))
                    blog.ReadTime = updateDto.ReadTime;

                if (updateDto.Featured.HasValue)
                    blog.Featured = updateDto.Featured.Value;

                if (!string.IsNullOrEmpty(updateDto.Image))
                    blog.Image = updateDto.Image;

                if (updateDto.Tags != null)
                    blog.SetTagsArray(updateDto.Tags);

                blog.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload with author
                await _context.Entry(blog).Reference(b => b.Author).LoadAsync();

                return MapToDto(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteBlogAsync(int id)
        {
            try
            {
                var blog = await _context.Blogs.FindAsync(id);
                if (blog == null) return false;

                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog: {Id}", id);
                throw;
            }
        }

        public async Task<List<BlogCategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.Blogs
                    .GroupBy(b => b.Category)
                    .Select(g => new BlogCategoryDto
                    {
                        Name = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Count)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                throw;
            }
        }

        public async Task<List<string>> GetPopularTagsAsync()
        {
            try
            {
                var allTags = await _context.Blogs
                    .Select(b => b.Tags)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToListAsync();

                var tagCounts = new Dictionary<string, int>();

                foreach (var tagsJson in allTags)
                {
                    try
                    {
                        var tags = JsonSerializer.Deserialize<string[]>(tagsJson);
                        if (tags != null)
                        {
                            foreach (var tag in tags)
                            {
                                if (tagCounts.ContainsKey(tag))
                                    tagCounts[tag]++;
                                else
                                    tagCounts[tag] = 1;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid JSON
                    }
                }

                return tagCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular tags");
                throw;
            }
        }

        public async Task IncrementViewsAsync(int id, string? userId, string? ipAddress)
        {
            try
            {
                var blog = await _context.Blogs.FindAsync(id);
                if (blog == null) return;

                blog.Views++;

                // Record view
                var blogView = new BlogView
                {
                    BlogId = id,
                    UserId = userId,
                    IpAddress = ipAddress,
                    ViewedAt = DateTime.UtcNow
                };

                _context.BlogViews.Add(blogView);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing views for blog: {Id}", id);
                throw;
            }
        }

        public async Task<BlogLikeResponseDto> ToggleLikeAsync(int id, string userId)
        {
            try
            {
                var existingLike = await _context.BlogLikes
                    .FirstOrDefaultAsync(bl => bl.BlogId == id && bl.UserId == userId);

                if (existingLike != null)
                {
                    // Unlike
                    _context.BlogLikes.Remove(existingLike);
                    var blog = await _context.Blogs.FindAsync(id);
                    if (blog != null)
                    {
                        blog.Likes = Math.Max(0, blog.Likes - 1);
                    }
                }
                else
                {
                    // Like
                    var blogLike = new BlogLike
                    {
                        BlogId = id,
                        UserId = userId,
                        LikedAt = DateTime.UtcNow
                    };

                    _context.BlogLikes.Add(blogLike);
                    var blog = await _context.Blogs.FindAsync(id);
                    if (blog != null)
                    {
                        blog.Likes++;
                    }
                }

                await _context.SaveChangesAsync();

                var updatedBlog = await _context.Blogs.FindAsync(id);
                var isLiked = existingLike == null; // If we removed a like, user unliked it

                return new BlogLikeResponseDto
                {
                    Likes = updatedBlog?.Likes ?? 0,
                    IsLiked = isLiked
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for blog: {Id}", id);
                throw;
            }
        }

        public async Task<BlogStatsDto> GetStatsAsync()
        {
            try
            {
                var totalBlogs = await _context.Blogs.CountAsync();
                var totalAuthors = await _context.BlogAuthors.CountAsync();
                var totalViews = await _context.Blogs.SumAsync(b => b.Views);
                var totalLikes = await _context.Blogs.SumAsync(b => b.Likes);

                return new BlogStatsDto
                {
                    TotalBlogs = totalBlogs,
                    TotalAuthors = totalAuthors,
                    TotalViews = totalViews,
                    TotalLikes = totalLikes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blog stats");
                throw;
            }
        }

        public async Task<List<BlogAuthorDto>> GetFeaturedAuthorsAsync()
        {
            try
            {
                var authors = await _context.BlogAuthors
                    .Select(a => new BlogAuthorDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Avatar = a.Avatar,
                        Role = a.Role
                    })
                    .Take(5)
                    .ToListAsync();

                return authors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured authors");
                throw;
            }
        }

        private BlogDto MapToDto(Blog blog)
        {
            return new BlogDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Excerpt = blog.Excerpt,
                Content = blog.Content,
                Slug = blog.Slug,
                Category = blog.Category,
                Tags = blog.GetTagsArray(),
                PublishedAt = blog.PublishedAt,
                ReadTime = blog.ReadTime,
                Views = blog.Views,
                Likes = blog.Likes,
                Featured = blog.Featured,
                Image = blog.Image,
                CreatedAt = blog.CreatedAt,
                UpdatedAt = blog.UpdatedAt,
                Author = new BlogAuthorDto
                {
                    Id = blog.Author.Id,
                    Name = blog.Author.Name,
                    Avatar = blog.Author.Avatar,
                    Role = blog.Author.Role
                }
            };
        }

        private string GenerateSlug(string title)
        {
            return title.ToLower()
                .Replace(" ", "-")
                .Replace("đ", "d")
                .Replace("ă", "a")
                .Replace("â", "a")
                .Replace("ê", "e")
                .Replace("ô", "o")
                .Replace("ơ", "o")
                .Replace("ư", "u")
                .Replace("í", "i")
                .Replace("ì", "i")
                .Replace("ỉ", "i")
                .Replace("ĩ", "i")
                .Replace("ị", "i")
                .Replace("á", "a")
                .Replace("à", "a")
                .Replace("ả", "a")
                .Replace("ã", "a")
                .Replace("ạ", "a")
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ẻ", "e")
                .Replace("ẽ", "e")
                .Replace("ẹ", "e")
                .Replace("ó", "o")
                .Replace("ò", "o")
                .Replace("ỏ", "o")
                .Replace("õ", "o")
                .Replace("ọ", "o")
                .Replace("ú", "u")
                .Replace("ù", "u")
                .Replace("ủ", "u")
                .Replace("ũ", "u")
                .Replace("ụ", "u")
                .Replace("ý", "y")
                .Replace("ỳ", "y")
                .Replace("ỷ", "y")
                .Replace("ỹ", "y")
                .Replace("ỵ", "y")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(".", "")
                .Replace(",", "")
                .Replace(":", "")
                .Replace(";", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("{", "")
                .Replace("}", "")
                .Replace("\"", "")
                .Replace("'", "")
                .Replace("&", "and");
        }
    }
}

