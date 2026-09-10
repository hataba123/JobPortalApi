using System.Security.Claims;
using JobPortalApi.Models;
using JobPortalApi.Services.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Controllers.User;

[ApiController]
[Route("api/newsletter")]
[Authorize]
public sealed class NewsletterController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _audit;

    public NewsletterController(ApplicationDbContext context, IAuditLogService audit)
    {
        _context = context;
        _audit = audit;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe()
    {
        var userId = CurrentUserId();
        var email = await _context.Users.Where(user => user.Id == userId).Select(user => user.Email).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(email)) return NotFound();
        var subscription = await _context.NewsletterSubscriptions
            .IgnoreQueryFilters().FirstOrDefaultAsync(item => item.UserId == userId && item.Email == email);
        if (subscription == null)
        {
            subscription = new NewsletterSubscription
            {
                Id = Guid.NewGuid(), UserId = userId, Email = email, IsActive = true, SubscribedAt = DateTime.UtcNow
            };
            _context.NewsletterSubscriptions.Add(subscription);
        }
        else
        {
            subscription.IsActive = true;
            subscription.UnsubscribedAt = null;
        }
        _audit.Add("Newsletter.Subscribed", "NewsletterSubscription", subscription.Id.ToString("D"), null, new { subscription.UserId });
        await _context.SaveChangesAsync();
        return Ok(new { active = true });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe()
    {
        var userId = CurrentUserId();
        var subscription = await _context.NewsletterSubscriptions
            .FirstOrDefaultAsync(item => item.UserId == userId && item.IsActive);
        if (subscription == null) return Ok(new { active = false });
        subscription.IsActive = false;
        subscription.UnsubscribedAt = DateTime.UtcNow;
        _audit.Add("Newsletter.Unsubscribed", "NewsletterSubscription", subscription.Id.ToString("D"), new { active = true }, new { active = false });
        await _context.SaveChangesAsync();
        return Ok(new { active = false });
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
