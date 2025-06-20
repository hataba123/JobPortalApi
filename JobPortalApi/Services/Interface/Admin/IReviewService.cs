using JobPortalApi.DTOs.AdminReview;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IReviewService
    {
        Task<List<ReviewDto>> GetAllReviewsAsync();
        Task<ReviewDto?> GetReviewByIdAsync(Guid id);
        Task<bool> UpdateReviewAsync(Guid id, UpdateReviewDto dto);
        Task<bool> DeleteReviewAsync(Guid id);
    }
}
