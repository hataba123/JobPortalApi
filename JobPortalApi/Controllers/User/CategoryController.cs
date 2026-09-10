using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return category == null ? NotFound() : Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JobPortalApi.DTOs.Category.CreateCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] JobPortalApi.DTOs.Category.UpdateCategoryDto dto)
        {
            var result = await _categoryService.UpdateAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _categoryService.DeleteAsync(id);
            return !success ? NotFound() : NoContent();
        }
    }
}
