using JobPortalApi.DTOs.Apply;
using JobPortalApi.Models.Enums;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.User;

public interface IApplyService
{
    Task ApplyToJobAsync(Guid candidateId, JobApplicationRequest request);
    Task<List<CandidateApplicationDto>> GetCandidatesAppliedToJob(Guid employerId, Guid jobPostId);
    Task<PagedResultDto<CandidateApplicationDto>> GetCandidatesAppliedToJobPaged(Guid employerId, Guid jobPostId, PagedQuery query);
    Task<List<JobAppliedDto>> GetJobsAppliedByCandidate(Guid candidateId);
    Task<PagedResultDto<JobAppliedDto>> GetJobsAppliedByCandidatePaged(Guid candidateId, PagedQuery query);
    Task<List<ApplyDto>> GetAllAsync();
    Task<PagedResultDto<ApplyDto>> GetAllPagedAsync(PagedQuery query);
    Task<ApplyDto?> GetByIdAsync(Guid id);
    Task<ApplyDto?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin, bool isRecruiter = false);
    Task<TransitionResultDto?> UpdateStatusAsync(Guid id, string status, Guid actorId, bool isAdmin, byte[]? expectedVersion, string? reason);
    Task<TransitionResultDto?> WithdrawAsync(Guid id, Guid candidateId, byte[]? expectedVersion, string? reason);
    Task<TransitionResultDto?> OverrideStatusAsync(Guid id, ApplyStatus status, Guid actorId, byte[]? expectedVersion, string reason);
    Task<List<ApplicationStatusHistoryDto>?> GetHistoryAsync(Guid id, Guid actorId, bool isAdmin);
    Task<bool> DeleteAsync(Guid id);
}
