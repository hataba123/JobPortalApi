using JobPortalApi.Services.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.Media;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly PublicMediaService _mediaService;

    public MediaController(PublicMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpPost("company-logo")]
    [Authorize(Roles = "Admin,Recruiter")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public Task<IActionResult> UploadCompanyLogo(IFormFile file, CancellationToken cancellationToken)
        => SaveAsync(file, "logo", cancellationToken);

    [HttpPost("blog-image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public Task<IActionResult> UploadBlogImage(IFormFile file, CancellationToken cancellationToken)
        => SaveAsync(file, "images", cancellationToken);

    private async Task<IActionResult> SaveAsync(IFormFile file, string directory, CancellationToken cancellationToken)
    {
        try
        {
            var url = await _mediaService.SaveImageAsync(file, directory, cancellationToken);
            return Ok(new { url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
