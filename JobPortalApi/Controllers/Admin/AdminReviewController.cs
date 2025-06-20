using JobPortalApi.DTOs.AdminReview;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/reviews")]
    public class AdminReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public AdminReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _reviewService.GetAllReviewsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await _reviewService.GetReviewByIdAsync(id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewDto dto)
        {
            var updated = await _reviewService.UpdateReviewAsync(id, dto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _reviewService.DeleteReviewAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
