using JobPortalApi.DTOs.Interview;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.User;

public interface IInterviewService
{
    Task<InterviewDto> CreateAsync(Guid applicationId, Guid actorId, bool isAdmin, CreateInterviewRequest request, byte[]? applicationVersion);
    Task<InterviewDto?> GetByIdAsync(Guid id, Guid actorId, bool isAdmin, bool isCandidate);
    Task<IReadOnlyList<InterviewDto>> GetAsync(Guid actorId, bool isAdmin, bool isCandidate, Guid? applicationId = null, string? status = null);
    Task<PagedResultDto<InterviewDto>> GetPagedAsync(Guid actorId, bool isAdmin, bool isCandidate, PagedQuery query, Guid? applicationId = null, string? status = null);
    Task<InterviewDto?> UpdateAsync(Guid id, Guid actorId, bool isAdmin, UpdateInterviewRequest request, byte[]? expectedVersion);
    Task<InterviewDto?> CompleteAsync(Guid id, Guid actorId, bool isAdmin, CompleteInterviewRequest request, byte[]? expectedInterviewVersion, byte[]? expectedApplicationVersion);
    Task<InterviewDto?> CancelAsync(Guid id, Guid actorId, bool isAdmin, byte[]? expectedVersion);
}
