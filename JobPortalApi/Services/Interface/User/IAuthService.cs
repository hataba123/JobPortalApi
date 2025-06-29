using JobPortalApi.DTOs.shared;

namespace JobPortalApi.Services.Interface.User
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterRequest request);
        Task<string> LoginAsync(LoginRequest request);
        Task<UserDto> GetUserByEmailAsync(string email); // 👈 Thêm hàm này

    }
}
