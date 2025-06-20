using JobPortalApi.DTOs.Employer;

namespace JobPortalApi.Services.Interface.User
{
    public interface IEmployerService
    {
        Task<List<EmployerJobDto>> GetMyPostedJobsAsync(Guid employerId);
        Task<bool> UpdateJobAsync(Guid jobId, Guid employerId, UpdateJobPostRequest request);
        Task<bool> DeleteJobAsync(Guid jobId, Guid employerId);
    }
}
