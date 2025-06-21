using JobPortalApi.DTOs.CandidateProfile;

namespace JobPortalApi.Services.Interface.User
{
    public interface ICandidateProfileService
    {
        Task<CandidateProfileDto?> GetByUserIdAsync(Guid userId);
        Task<bool> UpdateAsync(Guid userId, CandidateProfileUpdateDto dto);
    }
}
