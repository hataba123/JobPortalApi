using JobPortalApi.DTOs.AdminJobPost;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/jobposts")]
    public class AdminJobPostController : ControllerBase
    {
        private readonly IJobPostService _jobPostService;
        public AdminJobPostController(IJobPostService jobPostService) => _jobPostService = jobPostService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PagedQuery query) => Ok(await _jobPostService.GetAllJobPostsAsync(query));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var job = await _jobPostService.GetJobPostByIdAsync(id);
            if (job == null) return NotFound();
            Response.Headers.ETag = $"\"{job.Version}\"";
            return Ok(job);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateJobPostDto dto)
        {
            var newJ = await _jobPostService.CreateJobPostAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = newJ.Id }, newJ);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobPostDto dto, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = ReadRequiredVersion(ifMatch);
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            if (!await _jobPostService.UpdateJobPostAsync(id, dto, version)) return NotFound();
            var updated = await _jobPostService.GetJobPostByIdAsync(id);
            if (updated != null) Response.Headers.ETag = $"\"{updated.Version}\"";
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = ReadRequiredVersion(ifMatch);
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            return await _jobPostService.DeleteJobPostAsync(id, version) ? NoContent() : NotFound();
        }

        private static byte[]? ReadRequiredVersion(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : ConcurrencyToken.Decode(value.Trim());
    }
}
