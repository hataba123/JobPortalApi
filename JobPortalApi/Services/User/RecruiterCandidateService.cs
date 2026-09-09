using JobPortalApi.DTOs.CandidateProfile;
using JobPortalApi.DTOs.CandidateProfileDto;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using JobPortalApi.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortalApi.Services.User
{
    public class RecruiterCandidateService : IRecruiterCandidateService
    {
        private readonly ApplicationDbContext _context;

        public RecruiterCandidateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CandidateProfileBriefDto>> SearchCandidatesAsync(Guid recruiterId, CandidateSearchRequest request)
        {
            var query = _context.candidateProfiles
                .Include(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                query = query.Where(c => c.User.FullName.Contains(request.Keyword));
            }

            if (!string.IsNullOrEmpty(request.Skill))
            {
                query = query.Where(c => c.Skills.Contains(request.Skill));
            }

            if (!string.IsNullOrEmpty(request.Education))
            {
                query = query.Where(c => c.Education.Contains(request.Education));
            }

            if (request.MinYearsExperience.HasValue)
            {
                query = query.Where(c => c.Experience != null &&
                    c.Experience.Contains(request.MinYearsExperience.Value.ToString()));
            }

            return await query.Select(c => new CandidateProfileBriefDto
            {
                Id = c.Id,
                UserId = c.UserId,
                FullName = c.User.FullName,
                Skills = c.Skills,
                Experience = c.Experience,
                ExperienceYears = c.ExperienceYears,
                Education = c.Education,
                PreferredLocation = c.PreferredLocation,
                PreferredJobType = c.PreferredJobType,
                ExpectedSalary = c.ExpectedSalary
            }).ToListAsync();
        }

        public async Task<CandidateProfileDetailDto?> GetCandidateByIdAsync(Guid recruiterId, Guid candidateId)
        {
            var hasApplication = await _context.Jobs
                .AnyAsync(j => j.CandidateId == candidateId && j.JobPost.EmployerId == recruiterId);
            if (!hasApplication) return null;
            return await _context.candidateProfiles
                .Include(c => c.User)
                .Where(c => c.UserId == candidateId)
                .Select(c => new CandidateProfileDetailDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    FullName = c.User.FullName,
                    ResumeUrl = c.ResumeUrl,
                    Experience = c.Experience,
                    ExperienceYears = c.ExperienceYears,
                    Skills = c.Skills,
                    Education = c.Education,
                    PreferredLocation = c.PreferredLocation,
                    PreferredJobType = c.PreferredJobType,
                    ExpectedSalary = c.ExpectedSalary,
                    Dob = c.Dob,
                    Gender = c.Gender,
                    PortfolioUrl = c.PortfolioUrl,
                    LinkedinUrl = c.LinkedinUrl,
                    GithubUrl = c.GithubUrl,
                    Certificates = c.Certificates,
                    Summary = c.Summary,
                    Email = c.User.Email
                }).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CandidateApplicationDto>> GetCandidateApplicationsAsync(Guid recruiterId, Guid candidateId)
        {
            return await _context.Jobs
                .Include(j => j.JobPost)
                    .ThenInclude(jp => jp.Company)
                .Where(j =>
                    j.CandidateId == candidateId &&
                    j.JobPost.EmployerId == recruiterId
                )
                .Select(j => new CandidateApplicationDto
                {
                    JobId = j.Id,
                    JobPostId = j.JobPostId,
                    JobTitle = j.JobPost.Title,
                    AppliedAt = j.AppliedAt,
                    CVUrl = j.CVUrl,
                    Status = j.Status
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<CandidateProfileBriefDto>> GetCandidatesForRecruiterAsync(Guid recruiterId)
        {
            var candidateIds = await _context.Jobs
                .Include(j => j.JobPost)
                    .ThenInclude(jp => jp.Company)
                .Where(j =>
                    j.JobPost.EmployerId == recruiterId
                )
                .Select(j => j.CandidateId)
                .Distinct()
                .ToListAsync();

            return await _context.candidateProfiles
                .Include(c => c.User)
                .Where(c => candidateIds.Contains(c.UserId))
                .Select(c => new CandidateProfileBriefDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    FullName = c.User.FullName,
                    Skills = c.Skills,
                    Experience = c.Experience,
                    ExperienceYears = c.ExperienceYears,
                    Education = c.Education,
                    PreferredLocation = c.PreferredLocation,
                    PreferredJobType = c.PreferredJobType,
                    ExpectedSalary = c.ExpectedSalary
                }).ToListAsync();
        }

        public async Task<CandidateProfileDetailDto?> GetByUserIdAsync(Guid userId)
        {
            return await _context.candidateProfiles
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .Select(c => new CandidateProfileDetailDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    FullName = c.User.FullName,
                    ResumeUrl = c.ResumeUrl,
                    Experience = c.Experience,
                    ExperienceYears = c.ExperienceYears,
                    Skills = c.Skills,
                    Education = c.Education,
                    PreferredLocation = c.PreferredLocation,
                    PreferredJobType = c.PreferredJobType,
                    ExpectedSalary = c.ExpectedSalary,
                    Dob = c.Dob,
                    Gender = c.Gender,
                    PortfolioUrl = c.PortfolioUrl,
                    LinkedinUrl = c.LinkedinUrl,
                    GithubUrl = c.GithubUrl,
                    Certificates = c.Certificates,
                    Summary = c.Summary,
                    Email = c.User.Email
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(Guid userId, CandidateProfileUpdateDto dto)
        {
            var profile = await _context.candidateProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId); if (profile == null) return false;
            if (profile == null) return false;
            // Cập nhật các trường của CandidateProfile

            profile.Experience = dto.Experience;
            profile.ExperienceYears = dto.ExperienceYears;
            profile.Skills = dto.Skills;
            profile.Education = dto.Education;
            profile.PreferredLocation = dto.PreferredLocation;
            profile.PreferredJobType = dto.PreferredJobType;
            profile.ExpectedSalary = dto.ExpectedSalary;
            profile.Dob = dto.Dob;
            profile.Gender = dto.Gender;
            profile.PortfolioUrl = dto.PortfolioUrl;
            profile.LinkedinUrl = dto.LinkedinUrl;
            profile.GithubUrl = dto.GithubUrl;
            profile.Certificates = dto.Certificates;
            profile.Summary = dto.Summary;
            // Nếu có FullName hoặc Email thì cập nhật vào User
            if (!string.IsNullOrEmpty(dto.FullName))
                profile.User.FullName = dto.FullName;
            if (!string.IsNullOrEmpty(dto.Email))
                profile.User.Email = dto.Email;
            _context.candidateProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<string?> UploadCvAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File CV vượt quá dung lượng cho phép.");

            var profile = await _context.candidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return null;

            await using var input = file.OpenReadStream();
            await using var memory = new MemoryStream();
            await input.CopyToAsync(memory);
            var content = memory.ToArray();
            PrivateCvStorage.ValidatePdf(file.FileName, file.Length, content);

            Directory.CreateDirectory(PrivateCvStorage.RootDirectory);
            var fileName = $"{Guid.NewGuid():D}.pdf";
            var filePath = PrivateCvStorage.Resolve(fileName)
                ?? throw new InvalidOperationException("Không tạo được đường dẫn CV an toàn.");
            await System.IO.File.WriteAllBytesAsync(filePath, content);

            var oldPath = PrivateCvStorage.Resolve(profile.ResumeUrl);
            if (oldPath != null && System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);

            profile.ResumeUrl = fileName;
            _context.candidateProfiles.Update(profile);
            await _context.SaveChangesAsync();

            return "/api/candidate-profile/me/cv";
        }
        public async Task<bool> DeleteCvAsync(Guid userId)
        {
            var profile = await _context.candidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || string.IsNullOrEmpty(profile.ResumeUrl))
                return false;

            var filePath = PrivateCvStorage.Resolve(profile.ResumeUrl);
            if (filePath != null && File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            profile.ResumeUrl = null;
            _context.candidateProfiles.Update(profile);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(byte[] Content, string FileName)?> GetCvAsync(Guid actorId, Guid candidateId, bool isAdmin = false)
        {
            if (!isAdmin && !await _context.Jobs.AnyAsync(j =>
                    j.CandidateId == candidateId && j.JobPost.EmployerId == actorId))
                return null;

            var storageKey = await _context.candidateProfiles
                .Where(profile => profile.UserId == candidateId)
                .Select(profile => profile.ResumeUrl)
                .FirstOrDefaultAsync();
            var path = PrivateCvStorage.Resolve(storageKey);
            if (path == null || !File.Exists(path)) return null;
            return (await File.ReadAllBytesAsync(path), "resume.pdf");
        }


    }
}
