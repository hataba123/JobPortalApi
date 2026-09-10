using JobPortalApi.DTOs.AdminReview;

using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IReviewService
    {
        Task<List<ReviewDto>> GetAllReviewsAsync();
        Task<PagedResultDto<ReviewDto>> GetAllReviewsAsync(PagedQuery query);
        Task<ReviewDto?> GetReviewByIdAsync(Guid id);
        Task<bool> UpdateReviewAsync(Guid id, UpdateReviewDto dto);
        Task<bool> DeleteReviewAsync(Guid id);
    }
}
