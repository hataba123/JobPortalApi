using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetDashboard(Guid recruiterId)
        {
            var dashboard = await _dashboardService.GetDashboardAsync(recruiterId);
            return Ok(dashboard);
        }
    }
}
