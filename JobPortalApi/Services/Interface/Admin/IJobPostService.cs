using JobPortalApi.DTOs.AdminJobPost;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IJobPostService
    {
        Task<List<JobPostDto>> GetAllJobPostsAsync();
        Task<JobPostDto?> GetJobPostByIdAsync(Guid id);
        Task<JobPostDto> CreateJobPostAsync(CreateJobPostDto dto);
        Task<bool> UpdateJobPostAsync(Guid id, UpdateJobPostDto dto);
        Task<bool> DeleteJobPostAsync(Guid id);
    }
}
