using JobPortalApi.DTOs.Category;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    Color = c.Color
                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id)
        {
            return await _context.Categories
                .Where(c => c.Id == id)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    Color = c.Color
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var category = new Models.Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Icon = dto.Icon ?? string.Empty,
                Color = dto.Color ?? string.Empty
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Icon = category.Icon,
                Color = category.Color
            };
        }

        public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;

            if (dto.Name != null) category.Name = dto.Name;
            if (dto.Icon != null) category.Icon = dto.Icon;
            if (dto.Color != null) category.Color = dto.Color;

            await _context.SaveChangesAsync();

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Icon = category.Icon,
                Color = category.Color
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
