using System.Security.Claims;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.DTOs.shared;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortalApi.Controllers.Auth;

[EnableRateLimiting("auth")]
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var token = await _authService.RegisterAsync(request);
        var user = await _authService.GetUserByEmailAsync(request.Email);
        return Ok(new AuthResponse { Token = token, User = user });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await _authService.LoginAsync(request);
        var user = await _authService.GetUserByEmailAsync(request.Email);
        return Ok(new AuthResponse { Token = token, User = user });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized("Token không chứa email");
        var user = await _authService.GetUserByEmailAsync(email);
        return user == null ? NotFound("Không tìm thấy người dùng") : Ok(user);
    }

    [HttpPost("oauth-login")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthLogin(
        [FromBody] OAuthLoginRequest request,
        [FromHeader(Name = "X-OAuth-Exchange-Secret")] string exchangeSecret)
    {
        return Ok(await _authService.OAuthLoginAsync(request, exchangeSecret ?? string.Empty));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Đổi mật khẩu thành công." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.CreatePasswordResetRequestAsync(request.Email);
        return Ok(new { message = "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu sẽ được gửi." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Đặt lại mật khẩu thành công." });
    }
}
