using JobPortalApi.DTOs.CandidateProfile;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [Route("api/candidate-profile")]
    [ApiController]
    [Authorize(Roles = "Candidate")]
    public class CandidateProfileController : ControllerBase
    {
        private readonly ICandidateProfileService _profileService;

        public CandidateProfileController(ICandidateProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _profileService.GetByUserIdAsync(userId);
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] CandidateProfileUpdateDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _profileService.UpdateAsync(userId, dto);
            return success ? Ok("Cập nhật hồ sơ thành công") : NotFound("Không tìm thấy hồ sơ ứng viên");
        }
    }
}
