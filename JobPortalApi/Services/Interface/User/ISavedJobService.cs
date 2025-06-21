using JobPortalApi.DTOs.SavedJob;

namespace JobPortalApi.Services.Interface.User
{
    public interface ISavedJobService
    {
        Task<IEnumerable<SavedJobDto>> GetSavedJobsAsync(Guid userId);
        Task SaveJobAsync(Guid userId, Guid jobPostId);
        Task<bool> UnsaveJobAsync(Guid userId, Guid jobPostId);
    }
}
