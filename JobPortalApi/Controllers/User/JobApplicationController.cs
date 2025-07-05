using JobPortalApi.DTOs.Apply;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobApplicationController : ControllerBase
    {
        private readonly IApplyService _applyService;

        public JobApplicationController(IApplyService applyService)
        {
            _applyService = applyService;
        }

        // ✅ Candidate apply to a job
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Apply([FromBody] JobApplicationRequest request)
        {
            var candidateId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _applyService.ApplyToJobAsync(candidateId, request);
            return Ok(new { message = "Ứng tuyển thành công." });
        }

        // ✅ Recruiter: view candidates who applied to a job they posted
        [HttpGet("job/{jobPostId}/candidates")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetCandidatesForJob(Guid jobPostId)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _applyService.GetCandidatesAppliedToJob(recruiterId, jobPostId);
            return Ok(result);
        }

        // ✅ Candidate: view all jobs they applied to
        [HttpGet("my-jobs")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMyAppliedJobs()
        {
            var candidateId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _applyService.GetJobsAppliedByCandidate(candidateId);
            return Ok(result);
        }

        // ✅ Admin: get all apply records
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _applyService.GetAllAsync();
            return Ok(result);
        }

        // ✅ Admin: get a single apply record
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Candidate")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _applyService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // ✅ Recruiter/Admin: update status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateApplyStatusRequest request)
        {
            var success = await _applyService.UpdateStatusAsync(id, request.Status);
            if (!success) return NotFound();
            return Ok(new { message = "Cập nhật trạng thái thành công." });
        }

        // ✅ Admin: delete apply record
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _applyService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
