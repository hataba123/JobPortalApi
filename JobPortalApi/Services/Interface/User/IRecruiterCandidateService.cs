using JobPortalApi.DTOs.CandidateProfile;
using JobPortalApi.DTOs.CandidateProfileDto;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.User
{
    public interface IRecruiterCandidateService
    {
        Task<IEnumerable<CandidateProfileBriefDto>> SearchCandidatesAsync(Guid recruiterId, CandidateSearchRequest request);
        Task<PagedResultDto<CandidateProfileBriefDto>> SearchCandidatesPagedAsync(Guid recruiterId, CandidateSearchRequest request);
        Task<CandidateProfileDetailDto?> GetCandidateByIdAsync(Guid recruiterId, Guid candidateId);
        Task<IEnumerable<CandidateApplicationDto>> GetCandidateApplicationsAsync(Guid recruiterId, Guid candidateId); // ✅ Sửa ở đây
        Task<PagedResultDto<CandidateApplicationDto>> GetCandidateApplicationsPagedAsync(Guid recruiterId, Guid candidateId, PagedQuery query);
        Task<IEnumerable<CandidateProfileBriefDto>> GetCandidatesForRecruiterAsync(Guid recruiterId);
        Task<PagedResultDto<CandidateProfileBriefDto>> GetCandidatesForRecruiterPagedAsync(Guid recruiterId, PagedQuery query);
        Task<CandidateProfileDetailDto?> GetByUserIdAsync(Guid userId); // nếu controller cần
        Task<bool> UpdateAsync(Guid userId, CandidateProfileUpdateDto dto); // nếu controller cần
        Task<string?> UploadCvAsync(Guid userId, IFormFile file);
        Task<bool> DeleteCvAsync(Guid userId);
        Task<(byte[] Content, string FileName)?> GetCvAsync(Guid actorId, Guid candidateId, bool isAdmin = false);


    }
}
