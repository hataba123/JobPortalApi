using JobPortalApi.DTOs.Report;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.User;

public interface IJobReportService
{
    Task<JobReportDto> CreateAsync(Guid actorId, CreateJobReportRequest request);
    Task<IReadOnlyList<JobReportDto>> GetAllAsync(string? status = null);
    Task<PagedResultDto<JobReportDto>> GetAllAsync(PagedQuery query, string? status = null);
    Task<JobReportDto?> UpdateStatusAsync(Guid id, Guid actorId, string status, byte[]? expectedVersion = null);
}
