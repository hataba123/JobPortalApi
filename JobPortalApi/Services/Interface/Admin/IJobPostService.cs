using JobPortalApi.DTOs.AdminJobPost;

using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IJobPostService
    {
        Task<List<JobPostDto>> GetAllJobPostsAsync();
        Task<PagedResultDto<JobPostDto>> GetAllJobPostsAsync(PagedQuery query);
        Task<JobPostDto?> GetJobPostByIdAsync(Guid id);
        Task<JobPostDto> CreateJobPostAsync(CreateJobPostDto dto);
        Task<bool> UpdateJobPostAsync(Guid id, UpdateJobPostDto dto, byte[]? expectedVersion = null);
        Task<bool> DeleteJobPostAsync(Guid id, byte[]? expectedVersion = null);
    }
}
