using JobPortalApi.DTOs.JobPost;
using JobPortalApi.Models;
using JobPortalApi.Services;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using JobPortalApi.Services.Infrastructure;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobPostController : ControllerBase
    {
        private readonly IJobService _jobService;

        public JobPostController(IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] JobPostQuery query)
        {
            if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100 || query.MinSalary < 0)
                return BadRequest(new { message = "page phải >= 1 và pageSize phải trong khoảng 1-100." });
            var posts = await _jobService.GetAllAsync(query);
            return Ok(posts);
        }
        [HttpGet("company/{companyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCompanyId(Guid companyId)
        {
            var jobPosts = await _jobService.GetByCompanyIdAsync(companyId);
            return Ok(jobPosts); // Trả về 200 OK với danh sách job post
        }
        [HttpGet("category/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategoryId(Guid categoryId)
        {
            var result = await _jobService.GetByCategoryIdAsync(categoryId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var post = await _jobService.GetByIdAsync(id);
            if (post == null) return NotFound();
            Response.Headers.ETag = $"\"{post.Version}\"";
            return Ok(post);
        }

        [HttpGet("my-posts")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetMyPosts()
        {
            var recruiterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var posts = await _jobService.GetByEmployerIdAsync(recruiterId);
            return Ok(posts);
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Create(CreateJobPostDto dto)
        {
            var recruiterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var created = await _jobService.CreateAsync(dto, recruiterId);
            return Ok(created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Update(Guid id, UpdateJobPostDto dto, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = string.IsNullOrWhiteSpace(ifMatch) ? null : ConcurrencyToken.Decode(ifMatch.Trim());
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var updated = await _jobService.UpdateAsync(id, dto, recruiterId, version);
            if (updated == null) return NotFound();
            Response.Headers.ETag = $"\"{updated.Version}\"";
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Delete(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = string.IsNullOrWhiteSpace(ifMatch) ? null : ConcurrencyToken.Decode(ifMatch.Trim());
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _jobService.DeleteAsync(id, recruiterId, version);
            if (!success) return NotFound();
            return Ok(new { message = "Đã xóa mềm tin." });
        }
    }
}
