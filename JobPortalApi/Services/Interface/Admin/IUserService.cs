using JobPortalApi.DTOs.AdminUser;
using JobPortalApi.DTOs.shared;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<PagedResultDto<UserDto>> GetAllUsersAsync(PagedQuery query);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(Guid id);

    }
}
