using JobPortalApi.DTOs.Matching;
using JobPortalApi.Services.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/matches")]
    public class MatchingController : ControllerBase
    {
        private readonly MatchingService _matchingService;

        public MatchingController(MatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        [HttpGet("jobs")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetRecommendedJobs([FromQuery] MatchQueryDto query)
        {
            var candidateId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                return Ok(await _matchingService.GetRecommendedJobsAsync(candidateId, query));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("job-posts/{jobPostId}/candidates")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> RankCandidates(Guid jobPostId, [FromQuery] MatchQueryDto query)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                return Ok(await _matchingService.RankCandidatesAsync(actorId, jobPostId, User.IsInRole("Admin"), query));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("job-posts/{jobPostId}/candidates/{candidateId}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> GetCandidateMatch(Guid jobPostId, Guid candidateId)
        {
            var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                return Ok(await _matchingService.GetCandidateMatchAsync(actorId, jobPostId, candidateId, User.IsInRole("Admin")));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
