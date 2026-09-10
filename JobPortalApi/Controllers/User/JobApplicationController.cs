using System.Security.Claims;
using JobPortalApi.DTOs.Apply;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
public sealed class JobApplicationController : ControllerBase
{
    private readonly IApplyService _applyService;

    public JobApplicationController(IApplyService applyService) => _applyService = applyService;

    [HttpPost]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply([FromBody] JobApplicationRequest request)
    {
        await _applyService.ApplyToJobAsync(CurrentUserId(), request);
        return Ok(new { message = "Ứng tuyển thành công." });
    }

    [HttpGet("job/{jobPostId}/candidates")]
    [Authorize(Roles = "Recruiter")]
    public async Task<IActionResult> GetCandidatesForJob(Guid jobPostId, [FromQuery] PagedQuery? query = null)
    {
        if (query != null && (Request.Query.ContainsKey("page") || Request.Query.ContainsKey("pageSize")))
            return Ok(await _applyService.GetCandidatesAppliedToJobPaged(CurrentUserId(), jobPostId, query));
        var result = await _applyService.GetCandidatesAppliedToJob(CurrentUserId(), jobPostId);
        foreach (var application in result.Where(item => !string.IsNullOrEmpty(item.CVUrl)))
            application.CVUrl = $"/api/candidate-profile/recruiter/{application.CandidateId}/cv";
        return Ok(result);
    }

    [HttpGet("my-jobs")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyAppliedJobs([FromQuery] PagedQuery? query = null)
    {
        if (query != null && (Request.Query.ContainsKey("page") || Request.Query.ContainsKey("pageSize")))
            return Ok(await _applyService.GetJobsAppliedByCandidatePaged(CurrentUserId(), query));
        return Ok(await _applyService.GetJobsAppliedByCandidate(CurrentUserId()));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] PagedQuery query) => Ok(await _applyService.GetAllPagedAsync(query));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Candidate,Recruiter")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _applyService.GetByIdForUserAsync(
            id, CurrentUserId(), isAdmin, User.IsInRole("Recruiter"));
        if (result == null) return NotFound();
        if (!string.IsNullOrEmpty(result.CVUrl))
            result.CVUrl = User.IsInRole("Candidate")
                ? "/api/candidate-profile/me/cv"
                : $"/api/candidate-profile/recruiter/{result.CandidateId}/cv";
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = "Admin,Candidate,Recruiter")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var result = await _applyService.GetHistoryAsync(id, CurrentUserId(), User.IsInRole("Admin"));
        return result == null ? Forbid() : Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Recruiter")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateApplyStatusRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        var expectedVersion = ReadIfMatch(ifMatch);
        if (expectedVersion == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var result = await _applyService.UpdateStatusAsync(
            id, request.RequestedStatus, CurrentUserId(), User.IsInRole("Admin"), expectedVersion, request.Reason);
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Withdraw(
        Guid id,
        [FromBody] UpdateApplyStatusRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        var expectedVersion = ReadIfMatch(ifMatch);
        if (expectedVersion == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var result = await _applyService.WithdrawAsync(
            id, CurrentUserId(), expectedVersion, request.Reason);
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _applyService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static byte[]? ReadIfMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return ConcurrencyToken.Decode(value.Trim());
    }
}
