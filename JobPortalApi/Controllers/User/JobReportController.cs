using System.Security.Claims;
using JobPortalApi.DTOs.Report;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Controllers.User;

[ApiController]
[Route("api/reports")]
public sealed class JobReportController : ControllerBase
{
    private readonly IJobReportService _service;

    public JobReportController(IJobReportService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter,Candidate")]
    public async Task<IActionResult> Create([FromBody] CreateJobReportRequest request)
    {
        var result = await _service.CreateAsync(CurrentUserId(), request);
        return Created($"/api/reports/{result.Id:D}", result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] PagedQuery query) => Ok(await _service.GetAllAsync(query, status));

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateJobReportRequest request, [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch)) return StatusCode(StatusCodes.Status428PreconditionRequired);
        var result = await _service.UpdateStatusAsync(id, CurrentUserId(), request.Status, ConcurrencyToken.Decode(ifMatch.Trim()));
        return result == null ? NotFound() : Ok(result);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
