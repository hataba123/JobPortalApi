using JobPortalApi.DTOs.JobPost;
using JobPortalApi.Models;
using JobPortalApi.Services;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "page phải >= 1 và pageSize phải trong khoảng 1-100." });
            var posts = await _jobService.GetAllAsync(page, pageSize);
            return Ok(posts);
        }
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompanyId(Guid companyId)
        {
            var jobPosts = await _jobService.GetByCompanyIdAsync(companyId);
            return Ok(jobPosts); // Trả về 200 OK với danh sách job post
        }
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategoryId(Guid categoryId)
        {
            var result = await _jobService.GetByCategoryIdAsync(categoryId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var post = await _jobService.GetByIdAsync(id);
            if (post == null) return NotFound();
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
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Update(Guid id, UpdateJobPostDto dto)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var updated = await _jobService.UpdateAsync(id, dto, recruiterId);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _jobService.DeleteAsync(id, recruiterId);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
