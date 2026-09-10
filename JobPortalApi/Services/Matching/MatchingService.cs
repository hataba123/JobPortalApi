using System.Text.Json;
using System.Text.Encodings.Web;
using JobPortalApi.DTOs.Matching;
using JobPortalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Matching
{
    public class MatchingService
    {
        private readonly ApplicationDbContext _context;

        public MatchingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedMatchesDto> GetRecommendedJobsAsync(Guid candidateId, MatchQueryDto query)
        {
            var profile = await _context.candidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == candidateId);
            if (profile == null) throw new KeyNotFoundException("Hồ sơ ứng viên chưa tồn tại.");

            var jobs = await ActiveJobs().AsNoTracking().ToListAsync();
            var existingResults = await _context.MatchResults
                .Where(item => item.CandidateId == candidateId && jobs.Select(job => job.Id).Contains(item.JobPostId))
                .ToDictionaryAsync(item => item.JobPostId);
            var candidateInput = ToCandidateInput(profile);
            var matches = new List<MatchResultDto>();
            foreach (var job in jobs)
            {
                var result = MatchingEngine.Calculate(candidateInput, ToJobInput(job));
                UpsertResult(existingResults, result);
                matches.Add(WithJob(result, job));
            }
            await _context.SaveChangesAsync();

            var filtered = matches
                .Where(m => m.TotalScore >= query.MinScore)
                .OrderByDescending(m => m.TotalScore)
                .ToList();
            return Page(filtered, query);
        }

        public async Task<PagedMatchesDto> RankCandidatesAsync(Guid actorId, Guid jobPostId, bool isAdmin, MatchQueryDto query)
        {
            var job = await _context.JobPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobPostId);
            if (job == null) throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng.");
            if (!isAdmin && job.EmployerId != actorId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem xếp hạng tin này.");

            var applications = await _context.Jobs
                .Where(a => a.JobPostId == jobPostId)
                .Include(a => a.Candidate)
                .AsNoTracking()
                .ToListAsync();
            var candidateIds = applications.Select(a => a.CandidateId).ToArray();
            var profiles = await _context.candidateProfiles
                .Where(p => candidateIds.Contains(p.UserId))
                .AsNoTracking()
                .ToDictionaryAsync(p => p.UserId);
            var existingResults = await _context.MatchResults
                .Where(item => item.JobPostId == jobPostId && candidateIds.Contains(item.CandidateId))
                .ToDictionaryAsync(item => item.CandidateId);

            var matches = new List<MatchResultDto>();
            foreach (var application in applications)
            {
                if (!profiles.TryGetValue(application.CandidateId, out var profile)) continue;
                var result = MatchingEngine.Calculate(ToCandidateInput(profile), ToJobInput(job));
                UpsertResult(existingResults, result, result.CandidateId);
                matches.Add(new MatchResultDto
                {
                    JobPostId = result.JobPostId,
                    CandidateId = result.CandidateId,
                    TotalScore = result.TotalScore,
                    AlgorithmVersion = result.AlgorithmVersion,
                    Breakdown = ToBreakdownDto(result.Breakdown),
                    MatchedSkills = result.MatchedSkills.ToList(),
                    MissingSkills = result.MissingSkills.ToList(),
                    Reason = result.Reason,
                    InputFingerprint = result.InputFingerprint,
                    Candidate = new MatchCandidateSummaryDto
                    {
                        Id = application.Candidate.Id,
                        FullName = application.Candidate.FullName,
                        Email = application.Candidate.Email,
                    },
                });
            }
            await _context.SaveChangesAsync();

            var filtered = matches
                .Where(m => m.TotalScore >= query.MinScore)
                .OrderByDescending(m => m.TotalScore)
                .ToList();
            return Page(filtered, query);
        }

        public async Task<MatchResultDto> GetCandidateMatchAsync(Guid actorId, Guid jobPostId, Guid candidateId, bool isAdmin)
        {
            var job = await _context.JobPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobPostId);
            if (job == null) throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng.");
            if (!isAdmin && job.EmployerId != actorId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem xếp hạng tin này.");

            var item = await _context.Jobs
                .Where(a => a.JobPostId == jobPostId && a.CandidateId == candidateId)
                .Include(a => a.Candidate)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            if (item == null) throw new KeyNotFoundException("Ứng viên chưa ứng tuyển hoặc chưa có hồ sơ.");

            var profile = await _context.candidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == candidateId);
            if (profile == null) throw new KeyNotFoundException("Ứng viên chưa ứng tuyển hoặc chưa có hồ sơ.");

            var result = MatchingEngine.Calculate(ToCandidateInput(profile), ToJobInput(job));
            await UpsertResultAsync(result);
            await _context.SaveChangesAsync();
            return new MatchResultDto
            {
                JobPostId = result.JobPostId,
                CandidateId = result.CandidateId,
                TotalScore = result.TotalScore,
                AlgorithmVersion = result.AlgorithmVersion,
                Breakdown = ToBreakdownDto(result.Breakdown),
                MatchedSkills = result.MatchedSkills.ToList(),
                MissingSkills = result.MissingSkills.ToList(),
                Reason = result.Reason,
                InputFingerprint = result.InputFingerprint,
                Candidate = new MatchCandidateSummaryDto
                {
                    Id = item.Candidate.Id,
                    FullName = item.Candidate.FullName,
                    Email = item.Candidate.Email,
                },
            };
        }

        private IQueryable<JobPost> ActiveJobs()
        {
            var now = DateTime.UtcNow;
            return _context.JobPosts.Where(j => j.Status == Models.Enums.JobPostStatus.Active &&
                (!j.ExpiresAt.HasValue || j.ExpiresAt > now));
        }

        private async Task UpsertResultAsync(CalculatedMatch result)
        {
            var entity = await _context.MatchResults
                .FirstOrDefaultAsync(m => m.CandidateId == result.CandidateId && m.JobPostId == result.JobPostId);
            var existing = entity == null
                ? new Dictionary<Guid, MatchResult>()
                : new Dictionary<Guid, MatchResult> { [result.JobPostId] = entity };
            UpsertResult(existing, result);
        }

        private void UpsertResult(IReadOnlyDictionary<Guid, MatchResult> existingResults, CalculatedMatch result)
            => UpsertResult(existingResults, result, result.JobPostId);

        private void UpsertResult(
            IReadOnlyDictionary<Guid, MatchResult> existingResults,
            CalculatedMatch result,
            Guid lookupKey)
        {
            MatchResult entity;
            if (!existingResults.TryGetValue(lookupKey, out entity!))
            {
                entity = new MatchResult
                {
                    CandidateId = result.CandidateId,
                    JobPostId = result.JobPostId,
                };
                if (existingResults is IDictionary<Guid, MatchResult> mutableResults)
                    mutableResults[lookupKey] = entity;
                _context.MatchResults.Add(entity);
            }
            entity.TotalScore = result.TotalScore;
            entity.BreakdownJson = JsonSerializer.Serialize(ToBreakdownDto(result.Breakdown));
            entity.MatchedSkillsJson = JsonSerializer.Serialize(result.MatchedSkills);
            entity.MissingSkillsJson = JsonSerializer.Serialize(result.MissingSkills);
            entity.ReasonsJson = JsonSerializer.Serialize(new[] { result.Reason });
            entity.AlgorithmVersion = result.AlgorithmVersion;
            entity.InputFingerprint = result.InputFingerprint;
            entity.CreatedAt = DateTime.UtcNow;
        }

        private static PagedMatchesDto Page(List<MatchResultDto> matches, MatchQueryDto query)
        {
            return new PagedMatchesDto
            {
                Items = matches.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                Total = matches.Count,
            };
        }

        private static CandidateMatchInput ToCandidateInput(CandidateProfile profile) => new(
            profile.UserId,
            profile.Skills,
            profile.Certificates,
            profile.Experience,
            profile.ExperienceYears,
            profile.Education,
            profile.PreferredLocation,
            profile.PreferredJobType,
            profile.ExpectedSalary);

        private static JobMatchInput ToJobInput(JobPost job) => new(
            job.Id,
            job.SkillsRequired,
            JsonSerializer.Serialize(job.Tags ?? new List<string>(), new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }),
            job.Description,
            job.Location,
            job.Type,
            job.Salary,
            job.MinExperienceYears,
            job.EducationRequirement);

        private static MatchResultDto WithJob(CalculatedMatch result, JobPost job) => new()
        {
            JobPostId = result.JobPostId,
            CandidateId = result.CandidateId,
            TotalScore = result.TotalScore,
            AlgorithmVersion = result.AlgorithmVersion,
            Breakdown = ToBreakdownDto(result.Breakdown),
            MatchedSkills = result.MatchedSkills.ToList(),
            MissingSkills = result.MissingSkills.ToList(),
            Reason = result.Reason,
            InputFingerprint = result.InputFingerprint,
            JobPost = new MatchJobSummaryDto
            {
                Id = job.Id,
                Title = job.Title,
                Location = job.Location,
                Salary = job.Salary,
                Type = job.Type,
                ExpiresAt = job.ExpiresAt,
            },
        };

        private static MatchBreakdownDto ToBreakdownDto(MatchBreakdown breakdown) => new()
        {
            Skills = breakdown.Skills,
            Experience = breakdown.Experience,
            Education = breakdown.Education,
            Preferences = breakdown.Preferences,
        };
    }
}
