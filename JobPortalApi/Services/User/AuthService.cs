using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobPortalApi.Models;
using JobPortalApi.Helpers;
using JobPortalApi.DTOs.shared;
using JobPortalApi.Services.Interface.User;
using JobPortalApi.DTOs.Shared;
using System.Security.Cryptography;

namespace JobPortalApi.Services.User
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly SecureJwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _jwtHelper = new SecureJwtHelper(configuration);
        }

        // Đăng ký người dùng mới
        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            if (request.Role == UserRole.Admin)
                throw new InvalidOperationException("Không thể đăng ký công khai với vai trò Admin.");

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

            if (!VerifyPassword(request.Password, user.PasswordHash))
                throw new Exception("Email hoặc mật khẩu không đúng.");
            // Trả về JWT token
            return _jwtHelper.GenerateJwtToken(user); // ✅ Gọi helper
        }

        public async Task<AuthResponse> OAuthLoginAsync(
            OAuthLoginRequest request,
            string exchangeSecret)
        {
            var expectedSecret = _configuration["OAuth:ExchangeSecret"]
                ?? Environment.GetEnvironmentVariable("OAUTH_EXCHANGE_SECRET");
            if (string.IsNullOrWhiteSpace(expectedSecret) || exchangeSecret != expectedSecret)
                throw new UnauthorizedAccessException("OAuth exchange không hợp lệ.");

            var account = await _context.OAuthAccounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.Provider == request.Provider &&
                    a.ProviderAccountId == request.ProviderAccountId);

            Models.User user;
            if (account != null)
            {
                user = account.User;
            }
            else
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                    throw new InvalidOperationException("Email đã tồn tại. Hãy đăng nhập bằng mật khẩu trước khi liên kết OAuth.");

                user = new Models.User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    FullName = request.Name,
                    Role = UserRole.Candidate,
                    PasswordHash = HashPassword($"oauth:{request.Provider}:{request.ProviderAccountId}:{Guid.NewGuid()}")
                };
                _context.Users.Add(user);
                _context.OAuthAccounts.Add(new OAuthAccount
                {
                    UserId = user.Id,
                    Provider = request.Provider,
                    ProviderAccountId = request.ProviderAccountId
                });
                _context.candidateProfiles.Add(new CandidateProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id
                });
                await _context.SaveChangesAsync();
            }

            return new AuthResponse
            {
                Token = _jwtHelper.GenerateJwtToken(user),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                }
            };
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("Tài khoản không tồn tại.");
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordVersion++;
            await _context.SaveChangesAsync();
        }

        public async Task CreatePasswordResetRequestAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return;

            var now = DateTime.UtcNow;
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var activeTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null)
                .ToListAsync();
            foreach (var activeToken in activeTokens) activeToken.UsedAt = now;
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = HashResetToken(rawToken),
                ExpiresAt = now.AddMinutes(15)
            });
            await _context.SaveChangesAsync();
            // Token chỉ được gửi qua email provider; không trả raw token về API.
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var tokenHash = HashResetToken(request.Token);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
            if (resetToken == null || resetToken.UsedAt != null || resetToken.ExpiresAt <= DateTime.UtcNow ||
                resetToken.User.Email != request.Email)
                throw new InvalidOperationException("Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

            resetToken.UsedAt = DateTime.UtcNow;
            resetToken.User.PasswordHash = HashPassword(request.NewPassword);
            resetToken.User.PasswordVersion++;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
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

        private static string HashResetToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
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
