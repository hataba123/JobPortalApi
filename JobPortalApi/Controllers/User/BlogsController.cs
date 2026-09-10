using JobPortalApi.DTOs.Blog;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogs([FromQuery] BlogSearchDto searchDto)
        {
            var result = await _blogService.GetBlogsAsync(searchDto);
            return Ok(result);
        }

        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedBlogs()
        {
            var result = await _blogService.GetFeaturedBlogsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogById(int id)
        {
            var result = await _blogService.GetBlogByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogBySlug(string slug)
        {
            var result = await _blogService.GetBlogBySlugAsync(slug);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogDto createDto)
        {
            var result = await _blogService.CreateBlogAsync(createDto);
            return CreatedAtAction(nameof(GetBlogById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBlog(int id, [FromBody] UpdateBlogDto updateDto)
        {
            var result = await _blogService.UpdateBlogAsync(id, updateDto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var success = await _blogService.DeleteBlogAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _blogService.GetCategoriesAsync();
            return Ok(result);
        }

        [HttpGet("tags")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPopularTags()
        {
            var result = await _blogService.GetPopularTagsAsync();
            return Ok(result);
        }

        [HttpPost("{id}/views")]
        [Authorize]
        public async Task<IActionResult> IncrementViews(int id)
        {
            await _blogService.IncrementViewsAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok();
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var result = await _blogService.ToggleLikeAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(result);
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _blogService.GetStatsAsync();
            return Ok(result);
        }

        [HttpGet("authors/featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedAuthors()
        {
            var result = await _blogService.GetFeaturedAuthorsAsync();
            return Ok(result);
        }
    }
}
