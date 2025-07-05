using JobPortalApi.DTOs.CandidateProfile;
using JobPortalApi.DTOs.CandidateProfileDto;

namespace JobPortalApi.Services.Interface.User
{
    public interface IRecruiterCandidateService
    {
        Task<IEnumerable<CandidateProfileBriefDto>> SearchCandidatesAsync(Guid recruiterId, CandidateSearchRequest request);
        Task<CandidateProfileDetailDto?> GetCandidateByIdAsync(Guid recruiterId, Guid candidateId);
        Task<IEnumerable<CandidateApplicationDto>> GetCandidateApplicationsAsync(Guid recruiterId, Guid candidateId); // ✅ Sửa ở đây
        Task<IEnumerable<CandidateProfileBriefDto>> GetCandidatesForRecruiterAsync(Guid recruiterId);
        Task<CandidateProfileDetailDto?> GetByUserIdAsync(Guid userId); // nếu controller cần
        Task<bool> UpdateAsync(Guid userId, CandidateProfileUpdateDto dto); // nếu controller cần
        Task<string?> UploadCvAsync(Guid userId, IFormFile file);
        Task<bool> DeleteCvAsync(Guid userId);


    }
}
