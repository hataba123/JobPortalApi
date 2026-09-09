using JobPortalApi.DTOs.Shared;
using JobPortalApi.DTOs.shared;

namespace JobPortalApi.Services.Interface.User;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<string> LoginAsync(LoginRequest request);
    Task<UserDto> GetUserByEmailAsync(string email);
    Task<AuthResponse> OAuthLoginAsync(OAuthLoginRequest request, string exchangeSecret);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task CreatePasswordResetRequestAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
