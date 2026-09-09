using JobPortalApi.DTOs.Apply;

namespace JobPortalApi.Services.Interface.User
{
        public interface IApplyService
        {
            Task ApplyToJobAsync(Guid candidateId, JobApplicationRequest request);
            Task<List<CandidateApplicationDto>> GetCandidatesAppliedToJob(Guid employerId, Guid jobPostId);
            Task<List<JobAppliedDto>> GetJobsAppliedByCandidate(Guid candidateId);

            // CRUD mở rộng
            Task<List<ApplyDto>> GetAllAsync();
            Task<ApplyDto?> GetByIdAsync(Guid id);
            Task<ApplyDto?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin);
            Task<bool> UpdateStatusAsync(Guid id, string status, Guid actorId, bool isAdmin); // Update status (Accepted/Rejected...)
            Task<bool> DeleteAsync(Guid id);
        }
}
