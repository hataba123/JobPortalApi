using JobPortalApi.DTOs.Review;

namespace JobPortalApi.Services.Interface.User
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>> GetByCompanyAsync(Guid companyId);
        Task CreateAsync(Guid userId, CreateReviewRequest request);
        Task<IEnumerable<ReviewDto>> GetAllAsync();
    }
}
