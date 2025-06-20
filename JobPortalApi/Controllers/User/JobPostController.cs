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
        public async Task<IActionResult> GetAll()
        {
            var posts = await _jobService.GetAllAsync();
            return Ok(posts);
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
            var updated = await _jobService.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _jobService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
