using JobPortalApi.DTOs.AdminUser;
using JobPortalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.DTOs.shared;
using JobPortalApi.Services.Interface.User;
namespace JobPortalApi.Controllers.Auth
{
    [AllowAnonymous] // ✅ Cho phép truy cập không cần JWT
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Đăng ký tài khoản mới</summary>
        /// <returns>JWT token nếu thành công</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var token = await _authService.RegisterAsync(request);
                return Ok(new AuthResponse { Token = token });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); // có thể custom message thân thiện hơn
            }
        }

        /// <summary>Đăng nhập</summary>
        /// <returns>JWT token nếu thành công</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _authService.LoginAsync(request);
            var user = await _authService.GetUserByEmailAsync(request.Email); // ✅


            try
            {
                return Ok(new AuthResponse
                {
                    Token = token,
                    User = user
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        /// <summary>Lấy thông tin người dùng hiện tại</summary>
        [HttpGet("me")]
        [Authorize] // ✅ Cần token hợp lệ
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Me()
        {
            // 👇 Lấy email từ JWT Claims
            var email = User.FindFirstValue(ClaimTypes.Email);
            Console.WriteLine("🎯 Email from token: " + email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Token không chứa email");

            // 👇 Gọi service để lấy thông tin user
            var user = await _authService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            return Ok(user); // Trả về UserDto
        }

        [HttpPost("oauth-login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> OAuthLogin([FromBody] OAuthLoginRequest request)
        {
            try
            {
                // Kiểm tra user đã tồn tại chưa
                var existingUser = await _authService.GetUserByEmailAsync(request.Email);

                if (existingUser == null)
                {
                    // Nếu chưa có, tạo user mới (tùy vai trò mặc định)
                    var registerDto = new RegisterRequest
                    {
                        Email = request.Email,
                        FullName = request.Name,
                        Password = Guid.NewGuid().ToString(), // Random password vì không cần dùng
                        Role = UserRole.Candidate // hoặc cho phép FE gửi Role nếu muốn phân loại
                    };

                    var token = await _authService.RegisterAsync(registerDto);
                    var user = await _authService.GetUserByEmailAsync(request.Email);

                    return Ok(new AuthResponse
                    {
                        Token = token,
                        User = user
                    });
                }
                else
                {
                    // Nếu đã có, đăng nhập luôn
                    var token = await _authService.LoginAsync(new LoginRequest
                    {
                        Email = request.Email,
                        Password = "", // Không cần vì bạn có thể skip validate password cho OAuth
                        IsOAuth = true // 👈 Đề xuất: truyền cờ để login bypass password nếu OAuth
                    });

                    return Ok(new AuthResponse
                    {
                        Token = token,
                        User = existingUser
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
