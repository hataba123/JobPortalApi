using System.Linq.Expressions;
using System.Security.Claims;
using JobPortalApi.DTOs.Company;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Controllers.User;

[ApiController]
[Authorize(Roles = "Candidate")]
[Route("api/company-follows")]
public sealed class CompanyFollowController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CompanyFollowController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyFollowDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var follows = await _context.CompanyFollows
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Company.VerificationStatus == CompanyVerificationStatus.Verified)
            .OrderByDescending(item => item.FollowedAt)
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);
        return Ok(follows);
    }

    [HttpPost("{companyId:guid}")]
    public async Task<ActionResult<CompanyFollowDto>> Follow(Guid companyId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var companyExists = await _context.Companies.AnyAsync(
            company => company.Id == companyId && company.VerificationStatus == CompanyVerificationStatus.Verified,
            cancellationToken);
        if (!companyExists) return NotFound(new { message = "Công ty không tồn tại hoặc chưa được xác minh." });

        var follow = await _context.CompanyFollows
            .FirstOrDefaultAsync(item => item.UserId == userId && item.CompanyId == companyId, cancellationToken);
        if (follow == null)
        {
            follow = new CompanyFollow
            {
                UserId = userId,
                CompanyId = companyId,
                FollowedAt = DateTime.UtcNow
            };
            _context.CompanyFollows.Add(follow);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Hai tab có thể bấm theo dõi đồng thời; bản ghi duy nhất đã có
                // thì kết quả cuối cùng vẫn là đang theo dõi.
                _context.Entry(follow).State = EntityState.Detached;
                follow = await _context.CompanyFollows
                    .FirstAsync(item => item.UserId == userId && item.CompanyId == companyId, cancellationToken);
            }
        }

        var result = await _context.CompanyFollows
            .AsNoTracking()
            .Where(item => item.Id == follow.Id)
            .Select(ToDtoExpression())
            .SingleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{companyId:guid}")]
    public async Task<IActionResult> Unfollow(Guid companyId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _context.CompanyFollows
            .Where(item => item.UserId == userId && item.CompanyId == companyId)
            .ExecuteDeleteAsync(cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ.");
    }

    private static Expression<Func<CompanyFollow, CompanyFollowDto>> ToDtoExpression()
        => item => new CompanyFollowDto
        {
            Id = item.Id,
            CompanyId = item.CompanyId,
            Name = item.Company.Name,
            Logo = item.Company.Logo,
            Industry = item.Company.Industry,
            Location = item.Company.Location,
            FollowedAt = item.FollowedAt
        };
}
