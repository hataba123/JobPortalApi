using JobPortalApi.DTOs.AdminUser;
using JobPortalApi.DTOs.shared;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(Guid id);

    }
}