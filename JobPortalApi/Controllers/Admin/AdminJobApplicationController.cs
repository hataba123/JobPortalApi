using System.Security.Claims;
using JobPortalApi.DTOs.Apply;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.Admin;

[ApiController]
[Route("api/admin/jobapplications")]
[Authorize(Roles = "Admin")]
public sealed class AdminJobApplicationController : ControllerBase
{
    private readonly IApplyService _applyService;

    public AdminJobApplicationController(IApplyService applyService) => _applyService = applyService;

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> OverrideStatus(
        Guid id,
        [FromBody] UpdateApplyStatusRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        if (!Enum.TryParse<ApplyStatus>(request.RequestedStatus, true, out var status))
            return BadRequest(new { message = "Trạng thái hồ sơ không hợp lệ." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Admin override bắt buộc phải có lý do." });
        if (string.IsNullOrWhiteSpace(ifMatch))
            return StatusCode(StatusCodes.Status428PreconditionRequired);

        var result = await _applyService.OverrideStatusAsync(
            id,
            status,
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            ConcurrencyToken.Decode(ifMatch.Trim()),
            request.Reason);
        if (result == null) return NotFound();
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }
}
