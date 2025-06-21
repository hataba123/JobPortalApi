using JobPortalApi.DTOs.Category;

namespace JobPortalApi.Services.Interface.User
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(Guid id);
    }
}
