using JobPortalApi.DTOs.JobPost;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.Models;

namespace JobPortalApi.Services.Interface.User
{
    public interface IJobService
    {
        Task<PagedResponse<JobPostDto>> GetAllAsync(int page = 1, int pageSize = 20);
        Task<JobPostDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<JobPostDto>> GetByEmployerIdAsync(Guid employerId);
        Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId);
        Task<JobPostDto?> UpdateAsync(Guid id, UpdateJobPostDto dto, Guid employerId, byte[]? expectedVersion = null);
        Task<IEnumerable<JobPostDto>> GetByCompanyIdAsync(Guid companyId);

        Task<bool> DeleteAsync(Guid id, Guid employerId, byte[]? expectedVersion = null);
        Task<IEnumerable<JobPostDto>> GetByCategoryIdAsync(Guid categoryId);

    }
}
