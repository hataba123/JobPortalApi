using JobPortalApi.DTOs.CandidateProfile;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
namespace JobPortalApi.Services.User
{
    public class CandidateProfileService : ICandidateProfileService
    {
        private readonly ApplicationDbContext _context;

        public CandidateProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CandidateProfileDto?> GetByUserIdAsync(Guid userId)
        {
            return await _context.candidateProfiles
                .Where(c => c.UserId == userId)
                .Select(c => new CandidateProfileDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    ResumeUrl = c.ResumeUrl,
                    Experience = c.Experience,
                    Skills = c.Skills,
                    Education = c.Education,
                    Dob = c.Dob,
                    Gender = c.Gender,
                    PortfolioUrl = c.PortfolioUrl,
                    LinkedinUrl = c.LinkedinUrl,
                    GithubUrl = c.GithubUrl,
                    Certificates = c.Certificates,
                    Summary = c.Summary
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(Guid userId, CandidateProfileUpdateDto dto)
        {
            var profile = await _context.candidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return false;

            profile.ResumeUrl = dto.ResumeUrl;
            profile.Experience = dto.Experience;
            profile.Skills = dto.Skills;
            profile.Education = dto.Education;
            profile.Dob = dto.Dob;
            profile.Gender = dto.Gender;
            profile.PortfolioUrl = dto.PortfolioUrl;
            profile.LinkedinUrl = dto.LinkedinUrl;
            profile.GithubUrl = dto.GithubUrl;
            profile.Certificates = dto.Certificates;
            profile.Summary = dto.Summary;

            _context.candidateProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
