using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobPortalApi.Models;
using JobPortalApi.Helpers;
using JobPortalApi.DTOs.shared;
using JobPortalApi.Services.Interface.User;

namespace JobPortalApi.Services.User
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _jwtHelper = new JwtHelper(configuration); // Inject helper
        }

        // Đăng ký người dùng mới
        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                throw new Exception("Email đã được sử dụng.");

            // Tạo user mới với mật khẩu mã hoá
            var user = new Models.User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                Role = request.Role,
                PasswordHash = HashPassword(request.Password)
            };

            // Lưu user vào DB
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            // ✅ Nếu là Candidate thì tạo hồ sơ rỗng
            if (user.Role == UserRole.Candidate)
            {
                var candidateProfile = new CandidateProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ResumeUrl = null,
                    Experience = null,
                    Skills = null,
                    Education = null,
                    Dob = null,
                    Gender = null,
                    PortfolioUrl = null,
                    LinkedinUrl = null,
                    GithubUrl = null,
                    Certificates = null,
                    Summary = null
                };

                _context.candidateProfiles.Add(candidateProfile);
                await _context.SaveChangesAsync();
            }
            // Trả về JWT token
            return _jwtHelper.GenerateJwtToken(user); // ✅ Gọi helper
        }

        // Đăng nhập người dùng
        public async Task<string> LoginAsync(LoginRequest request)
        {
            // Tìm user theo email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Nếu không tìm thấy hoặc mật khẩu sai thì báo lỗi
            if (user == null)
                throw new Exception("Tài khoản không tồn tại.");

            if (!request.IsOAuth)
            {
                if (!VerifyPassword(request.Password, user.PasswordHash))
                    throw new Exception("Email hoặc mật khẩu không đúng.");
            }
            // Trả về JWT token
            return _jwtHelper.GenerateJwtToken(user); // ✅ Gọi helper
        }

        // Mã hoá mật khẩu bằng BCrypt
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // So sánh mật khẩu nhập với mật khẩu đã hash
        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, hashedPassword);
        }
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role,
                    FullName = u.FullName
                })
                .FirstOrDefaultAsync();

            return user;
        }
    }
}
