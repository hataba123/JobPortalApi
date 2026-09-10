using System.Security.Claims;
using JobPortalApi.DTOs.Interview;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.User;

[ApiController]
[Route("api/interviews")]
[Authorize(Roles = "Admin,Recruiter,Candidate")]
public sealed class InterviewController : ControllerBase
{
    private readonly IInterviewService _service;

    public InterviewController(IInterviewService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedQuery query, [FromQuery] Guid? applicationId, [FromQuery] string? status)
    {
        return Ok(await _service.GetPagedAsync(
            CurrentUserId(), User.IsInRole("Admin"), User.IsInRole("Candidate"), query, applicationId, status));
    }

    [HttpGet("legacy")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetLegacy([FromQuery] Guid? applicationId, [FromQuery] string? status)
    {
        return Ok(await _service.GetAsync(
            CurrentUserId(), User.IsInRole("Admin"), User.IsInRole("Candidate"), applicationId, status));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(
            id, CurrentUserId(), User.IsInRole("Admin"), User.IsInRole("Candidate"));
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInterviewRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        var version = ReadRequiredVersion(ifMatch);
        if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var result = await _service.UpdateAsync(id, CurrentUserId(), User.IsInRole("Admin"), request, version);
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteInterviewRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        var interviewVersion = ReadRequiredVersion(ifMatch);
        if (interviewVersion == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
        byte[]? applicationVersion = null;
        if (!string.IsNullOrWhiteSpace(request.ApplicationVersion))
            applicationVersion = ConcurrencyToken.Decode(request.ApplicationVersion);
        var result = await _service.CompleteAsync(
            id, CurrentUserId(), User.IsInRole("Admin"), request, interviewVersion, applicationVersion);
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Cancel(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        var version = ReadRequiredVersion(ifMatch);
        if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var result = await _service.CancelAsync(id, CurrentUserId(), User.IsInRole("Admin"), version);
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static byte[]? ReadRequiredVersion(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ConcurrencyToken.Decode(value.Trim());
}
