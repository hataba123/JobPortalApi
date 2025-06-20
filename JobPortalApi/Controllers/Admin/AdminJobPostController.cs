using JobPortalApi.DTOs.AdminJobPost;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAll() => Ok(await _jobPostService.GetAllJobPostsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
            => (await _jobPostService.GetJobPostByIdAsync(id)) is var j && j != null ? Ok(j) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateJobPostDto dto)
        {
            var newJ = await _jobPostService.CreateJobPostAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = newJ.Id }, newJ);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobPostDto dto)
            => await _jobPostService.UpdateJobPostAsync(id, dto) ? NoContent() : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _jobPostService.DeleteJobPostAsync(id) ? NoContent() : NotFound();
    }
}
