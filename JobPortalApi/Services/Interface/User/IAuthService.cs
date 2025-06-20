using JobPortalApi.DTOs.shared;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<string> LoginAsync(LoginRequest request);
    Task<UserDto> GetUserByEmailAsync(string email); // 👈 Thêm hàm này

}
