using JobPortalApi.DTOs.CandidateProfile;
using System.Data;
using JobPortalApi.DTOs.CandidateProfileDto;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using JobPortalApi.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Services.Media;

namespace JobPortalApi.Services.User
{
    public class RecruiterCandidateService : IRecruiterCandidateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOutboxService _outbox;
        private readonly IClamAvScanner _clamAvScanner;

        public RecruiterCandidateService(
            ApplicationDbContext context,
            IOutboxService outbox,
            IClamAvScanner clamAvScanner)
        {
            _context = context;
            _outbox = outbox;
            _clamAvScanner = clamAvScanner;
        }

        public async Task<IEnumerable<CandidateProfileBriefDto>> SearchCandidatesAsync(Guid recruiterId, CandidateSearchRequest request)
        {
            var result = await SearchCandidatesPagedAsync(recruiterId, request);
            return result.Items;
        }

        public async Task<PagedResultDto<CandidateProfileBriefDto>> SearchCandidatesPagedAsync(Guid recruiterId, CandidateSearchRequest request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            // Hồ sơ ẩn vẫn có thể được xem trong hồ sơ đã ứng tuyển, nhưng không
            // xuất hiện trong tìm kiếm chủ động của nhà tuyển dụng.
            var query = _context.candidateProfiles
                .AsNoTracking()
                .Where(candidate => candidate.User.ProfileVisibility);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(c => c.User.FullName.Contains(keyword) || c.User.Email.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(request.Skill))
            {
                var normalized = request.Skill.Trim().ToUpper();
                query = query.Where(c => _context.CandidateSkills.Any(skill =>
                    skill.CandidateProfileId == c.Id && skill.NormalizedName == normalized) ||
                    (c.Skills != null && c.Skills.Contains(request.Skill.Trim())));
            }

            if (!string.IsNullOrWhiteSpace(request.Education))
                query = query.Where(c => c.Education != null && c.Education.Contains(request.Education.Trim()));
            if (!string.IsNullOrWhiteSpace(request.Location))
                query = query.Where(c => c.PreferredLocation != null && c.PreferredLocation.Contains(request.Location.Trim()));

            var experienceFrom = request.ExperienceFrom ?? request.MinYearsExperience;
            if (experienceFrom.HasValue)
                query = query.Where(c => c.ExperienceYears >= experienceFrom.Value);
            if (request.ExperienceTo.HasValue)
                query = query.Where(c => c.ExperienceYears <= request.ExperienceTo.Value);

            var total = await query.CountAsync();
            var ordered = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
                ? request.SortBy?.ToLowerInvariant() switch
                {
                    "experience" => query.OrderBy(c => c.ExperienceYears),
                    "name" => query.OrderBy(c => c.User.FullName),
                    "location" => query.OrderBy(c => c.PreferredLocation),
                    _ => query.OrderBy(c => c.Id)
                }
                : request.SortBy?.ToLowerInvariant() switch
                {
                    "experience" => query.OrderByDescending(c => c.ExperienceYears),
                    "name" => query.OrderByDescending(c => c.User.FullName),
                    "location" => query.OrderByDescending(c => c.PreferredLocation),
                    _ => query.OrderByDescending(c => c.Id)
                };

            var items = await ordered.ThenBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new PagedResultDto<CandidateProfileBriefDto>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            };
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
                    Status = j.Status,
                    Version = ConcurrencyToken.Encode(j.RowVersion)
                })
                .ToListAsync();
        }

        public async Task<PagedResultDto<CandidateApplicationDto>> GetCandidateApplicationsPagedAsync(
            Guid recruiterId, Guid candidateId, PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.Jobs.AsNoTracking()
                .Where(j => j.CandidateId == candidateId && j.JobPost.EmployerId == recruiterId);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(j => j.AppliedAt)
                .ThenBy(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new CandidateApplicationDto
                {
                    JobId = j.Id,
                    JobPostId = j.JobPostId,
                    JobTitle = j.JobPost.Title,
                    AppliedAt = j.AppliedAt,
                    CVUrl = j.CVUrl,
                    Status = j.Status,
                    Version = ConcurrencyToken.Encode(j.RowVersion)
                })
                .ToListAsync();
            return new PagedResultDto<CandidateApplicationDto>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            };
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

        public async Task<PagedResultDto<CandidateProfileBriefDto>> GetCandidatesForRecruiterPagedAsync(
            Guid recruiterId, PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var candidateIds = _context.Jobs
                .Where(j => j.JobPost.EmployerId == recruiterId)
                .Select(j => j.CandidateId)
                .Distinct();
            var query = _context.candidateProfiles.AsNoTracking()
                .Where(c => candidateIds.Contains(c.UserId));
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(c => c.User.FullName.Contains(search) || c.User.Email.Contains(search));
            }
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.User.FullName)
                .ThenBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            return new PagedResultDto<CandidateProfileBriefDto>
            {
                Items = items, TotalCount = total, Page = page, PageSize = pageSize
            };
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
            await SyncCandidateSkillsAsync(profile);
            // Nếu có FullName hoặc Email thì cập nhật vào User
            if (!string.IsNullOrEmpty(dto.FullName))
                profile.User.FullName = dto.FullName;
            if (!string.IsNullOrEmpty(dto.Email))
                profile.User.Email = dto.Email;
            _context.candidateProfiles.Update(profile);
            // Hồ sơ đổi thì chỉ xếp hạng lại cho ứng viên này ở worker nền.
            _outbox.Add(
                "matching.candidate.refresh",
                new { CandidateId = userId },
                $"matching:candidate:{userId:D}:profile:{Guid.NewGuid():D}");
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

            var temporaryPath = Path.Combine(
                PrivateCvStorage.RootDirectory,
                $"{Guid.NewGuid():D}.uploading");
            var oldPath = PrivateCvStorage.Resolve(profile.ResumeUrl);
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                try
                {
                    await System.IO.File.WriteAllBytesAsync(temporaryPath, content);
                    await _clamAvScanner.ScanAsync(temporaryPath);

                    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                    profile.ResumeUrl = fileName;
                    await _context.SaveChangesAsync();
                    System.IO.File.Move(temporaryPath, filePath);
                    await transaction.CommitAsync();
                }
                catch
                {
                    if (System.IO.File.Exists(temporaryPath))
                        System.IO.File.Delete(temporaryPath);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                    throw;
                }
            });

            // File cũ không còn được DB tham chiếu; cleanup sau commit là best-effort.
            if (oldPath != null && System.IO.File.Exists(oldPath))
            {
                try { System.IO.File.Delete(oldPath); }
                catch (IOException) { }
            }

            return "/api/candidate-profile/me/cv";
        }
        public async Task<bool> DeleteCvAsync(Guid userId)
        {
            var profile = await _context.candidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || string.IsNullOrEmpty(profile.ResumeUrl))
                return false;

            var filePath = PrivateCvStorage.Resolve(profile.ResumeUrl);
            profile.ResumeUrl = null;
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });

            // Xóa sau khi DB commit để DB không trỏ tới một file đã biến mất.
            if (filePath != null && File.Exists(filePath))
            {
                try { File.Delete(filePath); }
                catch (IOException) { }
            }

            return true;
        }

        private async Task SyncCandidateSkillsAsync(CandidateProfile profile)
        {
            var existing = await _context.CandidateSkills
                .Where(skill => skill.CandidateProfileId == profile.Id)
                .ToListAsync();
            _context.CandidateSkills.RemoveRange(existing);
            var skills = (profile.Skills ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(skill => skill.Trim())
                .Where(skill => skill.Length > 0)
                .GroupBy(skill => skill.ToUpperInvariant())
                .Select(group => new CandidateSkill
                {
                    Id = Guid.NewGuid(), CandidateProfileId = profile.Id,
                    NormalizedName = group.Key, DisplayName = group.First()
                });
            await _context.CandidateSkills.AddRangeAsync(skills);
        }

        public async Task<(byte[] Content, string FileName)?> GetCvAsync(Guid actorId, Guid candidateId, bool isAdmin = false)
        {
            var isCandidateSelf = actorId == candidateId;
            if (!isAdmin && !isCandidateSelf && !await _context.Jobs.AnyAsync(j =>
                    j.CandidateId == candidateId && j.JobPost.EmployerId == actorId))
                return null;

            var storageKey = await _context.candidateProfiles
                .Where(profile => profile.UserId == candidateId)
                .Select(profile => profile.ResumeUrl)
                .FirstOrDefaultAsync();
            var path = PrivateCvStorage.Resolve(storageKey);
            if (path == null || !File.Exists(path)) return null;
            await _clamAvScanner.ScanAsync(path);
            return (await File.ReadAllBytesAsync(path), "resume.pdf");
        }


    }
}
