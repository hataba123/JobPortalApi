using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/recruiter/dashboard")]
    public class RecruiterDashboardController : Controller
    {
        private readonly IRecruiterDashboardService _dashboardService;

        public RecruiterDashboardController(IRecruiterDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("{recruiterId}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetDashboard(Guid recruiterId)
        {
            // Không tin recruiterId do client truyền lên; chỉ dùng subject của JWT.
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dashboard = await _dashboardService.GetDashboardAsync(actorId);
            return Ok(dashboard);
        }
    }
}
