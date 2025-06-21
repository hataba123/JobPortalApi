using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/saved-jobs")]
    [Authorize(Roles = "Candidate")]
    public class SavedJobController : ControllerBase
    {
        private readonly ISavedJobService _savedJobService;

        public SavedJobController(ISavedJobService savedJobService)
        {
            _savedJobService = savedJobService;
        }

        // Lấy danh sách job đã lưu
        [HttpGet]
        public async Task<IActionResult> GetSavedJobs()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var jobs = await _savedJobService.GetSavedJobsAsync(userId);
            return Ok(jobs);
        }

        // Lưu job
        [HttpPost("{jobPostId}")]
        public async Task<IActionResult> SaveJob(Guid jobPostId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _savedJobService.SaveJobAsync(userId, jobPostId);
            return Ok(new { message = "Lưu công việc thành công." });
        }

        // Bỏ lưu job
        [HttpDelete("{jobPostId}")]
        public async Task<IActionResult> UnsaveJob(Guid jobPostId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _savedJobService.UnsaveJobAsync(userId, jobPostId);
            return result ? Ok(new { message = "Đã xoá khỏi danh sách đã lưu." }) : NotFound();
        }
    }
}
