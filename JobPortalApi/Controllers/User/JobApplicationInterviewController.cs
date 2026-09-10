using System.Security.Claims;
using JobPortalApi.DTOs.Interview;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.User;

[ApiController]
[Route("api/jobapplication/{applicationId:guid}/interviews")]
[Authorize(Roles = "Admin,Recruiter")]
public sealed class JobApplicationInterviewController : ControllerBase
{
    private readonly IInterviewService _service;

    public JobApplicationInterviewController(IInterviewService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid applicationId,
        [FromBody] CreateInterviewRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return StatusCode(StatusCodes.Status428PreconditionRequired);
        var result = await _service.CreateAsync(
            applicationId,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.IsInRole("Admin"),
            request,
            ConcurrencyToken.Decode(ifMatch.Trim()));
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Created($"/api/interviews/{result.Id:D}", result);
    }
}
