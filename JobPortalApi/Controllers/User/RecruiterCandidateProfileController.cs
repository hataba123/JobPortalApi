using JobPortalApi.DTOs.CandidateProfile;
using JobPortalApi.DTOs.CandidateProfileDto;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [Route("api/candidate-profile")]
    [ApiController]
    public class RecruiterCandidateProfileController : Controller
    {
        private readonly IRecruiterCandidateService _candidateService;
        private readonly ApplicationDbContext _context;    // ← thêm


        public RecruiterCandidateProfileController(IRecruiterCandidateService candidateService, ApplicationDbContext context)
        {
            _candidateService = candidateService;
            _context = context;
        }

        // ===================== [CANDIDATE SELF] =========================

        [HttpGet("me")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _candidateService.GetByUserIdAsync(userId);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("me")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> UpdateProfile([FromBody] CandidateProfileUpdateDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _candidateService.UpdateAsync(userId, dto);
            return success ? Ok("Cập nhật hồ sơ thành công") : NotFound("Không tìm thấy hồ sơ ứng viên");
        }

        // ===================== [RECRUITER FUNCTIONS] =========================

        [HttpGet("recruiter/{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetCandidateById(Guid id)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _candidateService.GetCandidateByIdAsync(recruiterId, id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("recruiter/{id}/applications")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetCandidateApplications(Guid id)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _candidateService.GetCandidateApplicationsAsync(recruiterId, id);
            return Ok(result);
        }

        [HttpGet("recruiter/search")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> SearchCandidates([FromQuery] CandidateSearchRequest request)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _candidateService.SearchCandidatesAsync(recruiterId, request);
            return Ok(result);
        }

        [HttpGet("recruiter/applied")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetCandidatesAppliedToMyJobs()
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _candidateService.GetCandidatesForRecruiterAsync(recruiterId);
            return Ok(result);
        }
        [HttpPost("me/upload-cv")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> UploadCV(IFormFile file)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var url = await _candidateService.UploadCvAsync(userId, file);
            return url != null ? Ok(new { url }) : NotFound("Không tìm thấy đơn ứng tuyển.");
        }
        [HttpDelete("me/delete-cv")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> DeleteCv()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _candidateService.DeleteCvAsync(userId);
            return success ? Ok("Đã xóa CV thành công") : NotFound("Không tìm thấy đơn ứng tuyển để xóa CV.");
        }

    }
}
