using JobPortalApi.DTOs.Blog;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogsController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogsController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlogs([FromQuery] BlogSearchDto searchDto)
        {
            var result = await _blogService.GetBlogsAsync(searchDto);
            return Ok(result);
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedBlogs()
        {
            var result = await _blogService.GetFeaturedBlogsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlogById(int id)
        {
            var result = await _blogService.GetBlogByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBlogBySlug(string slug)
        {
            var result = await _blogService.GetBlogBySlugAsync(slug);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogDto createDto)
        {
            var result = await _blogService.CreateBlogAsync(createDto);
            return CreatedAtAction(nameof(GetBlogById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlog(int id, [FromBody] UpdateBlogDto updateDto)
        {
            var result = await _blogService.UpdateBlogAsync(id, updateDto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var success = await _blogService.DeleteBlogAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _blogService.GetCategoriesAsync();
            return Ok(result);
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetPopularTags()
        {
            var result = await _blogService.GetPopularTagsAsync();
            return Ok(result);
        }

        [HttpPost("{id}/views")]
        public async Task<IActionResult> IncrementViews(int id, [FromQuery] string? userId, [FromQuery] string? ipAddress)
        {
            await _blogService.IncrementViewsAsync(id, userId, ipAddress);
            return Ok();
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id, [FromQuery] string userId)
        {
            var result = await _blogService.ToggleLikeAsync(id, userId);
            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _blogService.GetStatsAsync();
            return Ok(result);
        }

        [HttpGet("authors/featured")]
        public async Task<IActionResult> GetFeaturedAuthors()
        {
            var result = await _blogService.GetFeaturedAuthorsAsync();
            return Ok(result);
        }
    }
}
