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
            if (result?.ResumeUrl != null)
                result.ResumeUrl = "/api/candidate-profile/me/cv";
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("me/cv")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> DownloadMyCv()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var file = await _candidateService.GetCvAsync(userId, userId);
            return file == null
                ? NotFound()
                : File(file.Value.Content, "application/pdf", file.Value.FileName);
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
            if (result?.ResumeUrl != null)
                result.ResumeUrl = $"/api/candidate-profile/recruiter/{id}/cv";
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("recruiter/{id}/applications")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetCandidateApplications(Guid id)
        {
            var recruiterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _candidateService.GetCandidateApplicationsAsync(recruiterId, id);
            foreach (var application in result)
            {
                if (!string.IsNullOrEmpty(application.CVUrl))
                    application.CVUrl = $"/api/candidate-profile/recruiter/{id}/cv";
            }
            return Ok(result);
        }

        [HttpGet("recruiter/{id}/cv")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> DownloadCandidateCv(Guid id)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var file = await _candidateService.GetCvAsync(actorId, id, User.IsInRole("Admin"));
            return file == null
                ? NotFound()
                : File(file.Value.Content, "application/pdf", file.Value.FileName);
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
            try
            {
                var url = await _candidateService.UploadCvAsync(userId, file);
                return url != null ? Ok(new { url }) : NotFound("Không tìm thấy hồ sơ ứng viên.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
