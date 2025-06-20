using JobPortalApi.DTOs.JobPost;
using JobPortalApi.Models;

namespace JobPortalApi.Services.Interface.User
{
    public interface IJobService
    {
        Task<IEnumerable<JobPostDto>> GetAllAsync();
        Task<JobPostDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<JobPostDto>> GetByEmployerIdAsync(Guid employerId);
        Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId);
        Task<JobPostDto?> UpdateAsync(Guid id, UpdateJobPostDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}