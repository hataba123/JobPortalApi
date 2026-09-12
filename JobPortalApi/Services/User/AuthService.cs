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
using JobPortalApi.Services.Notifications;
using JobPortalApi.Middleware;
using JobPortalApi.Services.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace JobPortalApi.Services.User
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly SecureJwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly OAuthProviderVerifier _oauthProviderVerifier;
        private readonly IPasswordHasher<Models.User> _legacyPasswordHasher;
        private readonly IOutboxService _outbox;
        private readonly IDataProtector _passwordResetProtector;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            OAuthProviderVerifier oauthProviderVerifier,
            IPasswordHasher<Models.User> legacyPasswordHasher,
            IOutboxService outbox,
            IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _configuration = configuration;
            _jwtHelper = new SecureJwtHelper(configuration);
            _oauthProviderVerifier = oauthProviderVerifier;
            _legacyPasswordHasher = legacyPasswordHasher;
            _outbox = outbox;
            _passwordResetProtector = dataProtectionProvider.CreateProtector("JobPortalApi.PasswordReset.v1");
        }

        // Đăng ký người dùng mới
        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            if (request.Role != UserRole.Candidate && request.Role != UserRole.Recruiter)
                throw new InvalidOperationException("Vai trò đăng ký không hợp lệ.");

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                throw new ApiConflictException("Email đã được sử dụng.");

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
            var email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
                throw new InvalidOperationException("Email hoặc mật khẩu không đúng.");
            // Tìm user theo email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Nếu không tìm thấy hoặc mật khẩu sai thì báo lỗi
            if (user == null)
                throw new InvalidOperationException("Email hoặc mật khẩu không đúng.");

            var now = DateTime.UtcNow;
            if (user.LockoutUntil > now)
                throw new InvalidOperationException("Email hoặc mật khẩu không đúng.");

            if (!VerifyPassword(request.Password, user, out var usedLegacyPasswordHash))
            {
                user.FailedLoginAttempts++;
                var maxAttempts = GetPositiveSetting("AUTH_MAX_FAILED_LOGIN_ATTEMPTS", "Auth:MaxFailedLoginAttempts", 5);
                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    var lockoutMinutes = GetPositiveSetting("AUTH_LOCKOUT_MINUTES", "Auth:LockoutMinutes", 15);
                    user.LockoutUntil = now.AddMinutes(lockoutMinutes);
                }
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Email hoặc mật khẩu không đúng.");
            }

            if (usedLegacyPasswordHash)
                user.PasswordHash = HashPassword(request.Password);

            if (usedLegacyPasswordHash || user.FailedLoginAttempts != 0 || user.LockoutUntil.HasValue)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutUntil = null;
                await _context.SaveChangesAsync();
            }
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

            var identity = await _oauthProviderVerifier.VerifyAsync(
                request.Provider,
                request.AccessToken);

            var account = await _context.OAuthAccounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.Provider == identity.Provider &&
                    a.ProviderAccountId == identity.ProviderAccountId);

            Models.User user;
            if (account != null)
            {
                user = account.User;
                if (user.DeletedAt.HasValue)
                    throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa.");
            }
            else
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == identity.Email);
                if (existingUser != null)
                    throw new InvalidOperationException("Email đã tồn tại. Hãy đăng nhập bằng mật khẩu trước khi liên kết OAuth.");

                user = new Models.User
                {
                    Id = Guid.NewGuid(),
                    Email = identity.Email,
                    FullName = identity.Name,
                    Role = UserRole.Candidate,
                    PasswordHash = HashPassword($"oauth:{identity.Provider}:{identity.ProviderAccountId}:{Guid.NewGuid()}")
                };
                _context.Users.Add(user);
                _context.OAuthAccounts.Add(new OAuthAccount
                {
                    UserId = user.Id,
                    Provider = identity.Provider,
                    ProviderAccountId = identity.ProviderAccountId
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
            if (!VerifyPassword(request.CurrentPassword, user, out _))
                throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordVersion++;
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
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
            // Token thô chỉ nằm trong payload được Data Protection mã hóa. Database
            // vẫn chỉ lưu hash token để kiểm tra hạn và vô hiệu hóa token đã dùng.
            _outbox.Add("password.reset.requested", new
            {
                Email = user.Email,
                ProtectedToken = _passwordResetProtector.Protect(rawToken)
            }, $"password-reset:{user.Id:D}:{HashResetToken(rawToken)}");
            await _context.SaveChangesAsync();
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var tokenHash = HashResetToken(request.Token);
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
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
                resetToken.User.FailedLoginAttempts = 0;
                resetToken.User.LockoutUntil = null;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }

        // Mã hoá mật khẩu bằng BCrypt
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Hỗ trợ một lần cho tài khoản lịch sử được tạo bằng ASP.NET Identity,
        // sau lần đăng nhập thành công hash được thay bằng BCrypt.
        private bool VerifyPassword(string inputPassword, Models.User user, out bool usedLegacyPasswordHash)
        {
            usedLegacyPasswordHash = false;
            if (string.IsNullOrWhiteSpace(user.PasswordHash)) return false;

            if (user.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
                return BCrypt.Net.BCrypt.Verify(inputPassword, user.PasswordHash);

            var result = _legacyPasswordHasher.VerifyHashedPassword(user, user.PasswordHash, inputPassword);
            usedLegacyPasswordHash = result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
            return usedLegacyPasswordHash;
        }

        private static string HashResetToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        private int GetPositiveSetting(string environmentKey, string configurationKey, int defaultValue)
        {
            var rawValue = Environment.GetEnvironmentVariable(environmentKey) ?? _configuration[configurationKey];
            return int.TryParse(rawValue, out var value) && value > 0 ? value : defaultValue;
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
