using System.Security.Claims;
using JobPortalApi.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Controllers.User;

[ApiController]
[Authorize]
[Route("api/user-settings")]
public sealed class UserSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserSettingsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var settings = await _context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserSettingsDto
            {
                Phone = user.Phone,
                EmailNotifications = user.EmailNotifications,
                JobAlerts = user.JobAlerts,
                MarketingEmails = user.MarketingEmails,
                ProfileVisibility = user.ProfileVisibility,
                ApplicationUpdates = user.ApplicationUpdates
            })
            .SingleOrDefaultAsync(cancellationToken);
        return settings == null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<UserSettingsDto>> Update(
        [FromBody] UserSettingsDto request,
        CancellationToken cancellationToken)
    {
        if (request.Phone?.Length > 30)
            return BadRequest(new { message = "Số điện thoại không được vượt quá 30 ký tự." });

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == GetUserId(), cancellationToken);
        if (user == null) return NotFound();

        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.EmailNotifications = request.EmailNotifications;
        user.JobAlerts = request.JobAlerts;
        user.MarketingEmails = request.MarketingEmails;
        user.ProfileVisibility = request.ProfileVisibility;
        user.ApplicationUpdates = request.ApplicationUpdates;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new UserSettingsDto
        {
            Phone = user.Phone,
            EmailNotifications = user.EmailNotifications,
            JobAlerts = user.JobAlerts,
            MarketingEmails = user.MarketingEmails,
            ProfileVisibility = user.ProfileVisibility,
            ApplicationUpdates = user.ApplicationUpdates
        });
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ.");
    }
}
