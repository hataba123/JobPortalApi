using JobPortalApi.DTOs.Category;

namespace JobPortalApi.Services.Interface.User
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(Guid id);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
