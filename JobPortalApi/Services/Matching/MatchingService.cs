using System.Text.Json;
using System.Text.Encodings.Web;
using JobPortalApi.DTOs.Matching;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Matching
{
    public class MatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOutboxService _outbox;

        public MatchingService(ApplicationDbContext context, IOutboxService outbox)
        {
            _context = context;
            _outbox = outbox;
        }

        public async Task<PagedMatchesDto> GetRecommendedJobsAsync(Guid candidateId, MatchQueryDto query, CancellationToken cancellationToken = default)
        {
            var profile = await _context.candidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == candidateId, cancellationToken);
            if (profile == null) throw new KeyNotFoundException("Hồ sơ ứng viên chưa tồn tại.");

            var now = DateTime.UtcNow;
            var resultQuery = _context.MatchResults
                .AsNoTracking()
                .Where(item => item.CandidateId == candidateId && item.TotalScore >= query.MinScore)
                .Where(item => item.JobPost.Status == JobPostStatus.Active &&
                    (!item.JobPost.ExpiresAt.HasValue || item.JobPost.ExpiresAt > now));
            var total = await resultQuery.CountAsync(cancellationToken);
            var results = await resultQuery
                .OrderByDescending(item => item.TotalScore)
                .ThenBy(item => item.JobPostId)
                .Include(item => item.JobPost)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var pending = await HasPendingCandidateRefreshAsync(candidateId, cancellationToken);
            if (!pending)
            {
                var refreshKey = await GetCandidateRefreshKeyAsync(candidateId, cancellationToken);
                if (refreshKey != null)
                    pending = await QueueCandidateRefreshIfNeededAsync(candidateId, refreshKey, cancellationToken);
            }
            return new PagedMatchesDto
            {
                Items = results.Select(ToDto).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                Total = total,
                IsPending = pending
            };
        }

        public async Task RefreshCandidateMatchesAsync(
            Guid candidateId,
            int batchSize,
            DateTime? beforeCreatedAt,
            Guid? afterJobId,
            CancellationToken cancellationToken)
        {
            var profile = await _context.candidateProfiles.AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == candidateId, cancellationToken);
            if (profile == null) return;

            var take = Math.Clamp(batchSize, 1, 500);
            var jobsQuery = ActiveJobs().AsNoTracking();
            if (beforeCreatedAt.HasValue && afterJobId.HasValue)
            {
                jobsQuery = jobsQuery.Where(item => item.CreatedAt < beforeCreatedAt.Value ||
                    (item.CreatedAt == beforeCreatedAt.Value && item.Id.CompareTo(afterJobId.Value) > 0));
            }

            var jobs = await jobsQuery
                .OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
                .Take(take + 1)
                .ToListAsync(cancellationToken);
            var hasMore = jobs.Count > take;
            if (hasMore) jobs = jobs.Take(take).ToList();
            if (jobs.Count == 0) return;
            var jobIds = jobs.Select(item => item.Id).ToArray();
            var existingResults = await _context.MatchResults
                .Where(item => item.CandidateId == candidateId && jobIds.Contains(item.JobPostId))
                .ToDictionaryAsync(item => item.JobPostId, cancellationToken);
            var candidateInput = ToCandidateInput(profile);
            foreach (var job in jobs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                UpsertResult(existingResults, MatchingEngine.Calculate(candidateInput, ToJobInput(job)));
            }
            await _context.SaveChangesAsync(cancellationToken);
            if (hasMore)
            {
                var last = jobs[^1];
                var key = $"matching:candidate:{candidateId:D}:after:{last.CreatedAt.Ticks}:{last.Id:D}";
                _outbox.Add("matching.candidate.refresh", new
                {
                    CandidateId = candidateId,
                    CursorCreatedAt = last.CreatedAt,
                    CursorJobId = last.Id
                }, key);
            }
        }

        public async Task<PagedMatchesDto> RankCandidatesAsync(Guid actorId, Guid jobPostId, bool isAdmin, MatchQueryDto query, CancellationToken cancellationToken = default)
        {
            var job = await _context.JobPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobPostId, cancellationToken);
            if (job == null) throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng.");
            if (!isAdmin && job.EmployerId != actorId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem xếp hạng tin này.");

            var resultQuery = _context.MatchResults.AsNoTracking()
                .Where(item => item.JobPostId == jobPostId && item.TotalScore >= query.MinScore);
            var total = await resultQuery.CountAsync(cancellationToken);
            var results = await resultQuery
                .OrderByDescending(item => item.TotalScore).ThenBy(item => item.CandidateId)
                .Include(item => item.Candidate)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .ToListAsync(cancellationToken);
            var pending = await HasPendingJobRefreshAsync(jobPostId, cancellationToken);
            if (!pending)
            {
                var refreshKey = await GetJobRankingRefreshKeyAsync(jobPostId, cancellationToken);
                if (refreshKey != null)
                    pending = await QueueJobRankingRefreshIfNeededAsync(jobPostId, refreshKey, cancellationToken);
            }
            return new PagedMatchesDto
            {
                Items = results.Select(ToDto).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                Total = total,
                IsPending = pending
            };
        }

        public async Task RefreshJobCandidateMatchesAsync(
            Guid jobPostId,
            int batchSize,
            Guid? afterApplicationId,
            CancellationToken cancellationToken)
        {
            var job = await _context.JobPosts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == jobPostId, cancellationToken);
            if (job == null) return;

            var take = Math.Clamp(batchSize, 1, 500);
            var applicationsQuery = _context.Jobs.Where(item => item.JobPostId == jobPostId);
            if (afterApplicationId.HasValue)
                applicationsQuery = applicationsQuery.Where(item => item.Id.CompareTo(afterApplicationId.Value) > 0);
            var applications = await applicationsQuery
                .OrderBy(item => item.Id).Take(take + 1).AsNoTracking().ToListAsync(cancellationToken);
            var hasMore = applications.Count > take;
            if (hasMore) applications = applications.Take(take).ToList();
            var candidateIds = applications.Select(item => item.CandidateId).Distinct().ToArray();
            var profiles = await _context.candidateProfiles.Where(item => candidateIds.Contains(item.UserId))
                .AsNoTracking().ToDictionaryAsync(item => item.UserId, cancellationToken);
            var existingResults = await _context.MatchResults.Where(item => item.JobPostId == jobPostId && candidateIds.Contains(item.CandidateId))
                .ToDictionaryAsync(item => item.CandidateId, cancellationToken);
            foreach (var candidateId in candidateIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!profiles.TryGetValue(candidateId, out var profile)) continue;
                UpsertResult(existingResults, MatchingEngine.Calculate(ToCandidateInput(profile), ToJobInput(job)), candidateId);
            }
            await _context.SaveChangesAsync(cancellationToken);
            if (hasMore)
            {
                var last = applications[^1];
                var key = $"matching:job:{jobPostId:D}:after:{last.Id:D}";
                _outbox.Add("matching.job.refresh", new
                {
                    JobPostId = jobPostId,
                    CursorApplicationId = last.Id
                }, key);
            }
        }

        public async Task<MatchResultDto> GetCandidateMatchAsync(Guid actorId, Guid jobPostId, Guid candidateId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var job = await _context.JobPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobPostId, cancellationToken);
            if (job == null) throw new KeyNotFoundException("Không tìm thấy tin tuyển dụng.");
            if (!isAdmin && job.EmployerId != actorId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem xếp hạng tin này.");

            var item = await _context.Jobs
                .Where(a => a.JobPostId == jobPostId && a.CandidateId == candidateId)
                .Include(a => a.Candidate)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
            if (item == null) throw new KeyNotFoundException("Ứng viên chưa ứng tuyển hoặc chưa có hồ sơ.");

            var profile = await _context.candidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == candidateId, cancellationToken);
            if (profile == null) throw new KeyNotFoundException("Ứng viên chưa ứng tuyển hoặc chưa có hồ sơ.");

            var result = MatchingEngine.Calculate(ToCandidateInput(profile), ToJobInput(job));
            await UpsertResultAsync(result);
            await _context.SaveChangesAsync(cancellationToken);
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
            else if (entity.AlgorithmVersion == result.AlgorithmVersion &&
                     string.Equals(entity.InputFingerprint, result.InputFingerprint, StringComparison.Ordinal))
            {
                // Hồ sơ và tin chưa đổi; giữ nguyên CreatedAt để worker không
                // ghi lại hàng loạt kết quả giống nhau.
                return;
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

        private async Task<string?> GetCandidateRefreshKeyAsync(Guid candidateId, CancellationToken cancellationToken)
        {
            var baseKey = $"matching:candidate:{candidateId:D}";
            var lastCompletedRefresh = await _context.OutboxMessages
                .AsNoTracking()
                .Where(message => message.Type == "matching.candidate.refresh" &&
                                  message.DeduplicationKey != null &&
                                  (message.DeduplicationKey == baseKey ||
                                   message.DeduplicationKey.StartsWith(baseKey + ":")) &&
                                  message.ProcessedAt != null)
                .MaxAsync(message => (DateTime?)message.ProcessedAt, cancellationToken);
            var newestJob = await ActiveJobs()
                .AsNoTracking()
                .OrderByDescending(job => job.CreatedAt)
                .ThenByDescending(job => job.Id)
                .Select(job => new { job.CreatedAt, job.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (newestJob == null)
                return lastCompletedRefresh.HasValue ? null : baseKey;
            if (!lastCompletedRefresh.HasValue || newestJob.CreatedAt > lastCompletedRefresh.Value)
                return $"{baseKey}:jobs:{newestJob.CreatedAt.Ticks}:{newestJob.Id:D}";
            return null;
        }

        private async Task<bool> QueueCandidateRefreshIfNeededAsync(
            Guid candidateId,
            string key,
            CancellationToken cancellationToken)
        {
            var existing = await _context.OutboxMessages
                .AsNoTracking()
                .Where(message => message.DeduplicationKey == key)
                .Select(message => new { message.ProcessedAt, message.DeadLetteredAt })
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                return existing.ProcessedAt == null && existing.DeadLetteredAt == null;

            _outbox.Add("matching.candidate.refresh", new { CandidateId = candidateId }, key);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsDuplicateDeduplicationKey(exception))
            {
                // Một request đồng thời đã tạo cùng message; message đó sẽ được worker xử lý.
                DetachPendingOutboxEntries();
            }
            return await HasPendingCandidateRefreshAsync(candidateId, cancellationToken);
        }

        private Task<bool> HasPendingCandidateRefreshAsync(Guid candidateId, CancellationToken cancellationToken)
            => _context.OutboxMessages.AnyAsync(message =>
                message.DeduplicationKey != null &&
                (message.DeduplicationKey == $"matching:candidate:{candidateId:D}" ||
                 message.DeduplicationKey.StartsWith($"matching:candidate:{candidateId:D}:")) &&
                message.ProcessedAt == null && message.DeadLetteredAt == null,
                cancellationToken);

        private async Task<string?> GetJobRankingRefreshKeyAsync(Guid jobPostId, CancellationToken cancellationToken)
        {
            var key = $"matching:job:{jobPostId:D}";
            var lastCompletedRefresh = await _context.OutboxMessages
                .AsNoTracking()
                .Where(message => message.Type == "matching.job.refresh" &&
                                  message.DeduplicationKey != null &&
                                  (message.DeduplicationKey == key ||
                                   message.DeduplicationKey.StartsWith(key + ":")) &&
                                  message.ProcessedAt != null)
                .MaxAsync(message => (DateTime?)message.ProcessedAt, cancellationToken);
            var newestApplication = await _context.Jobs
                .AsNoTracking()
                .Where(application => application.JobPostId == jobPostId)
                .OrderByDescending(application => application.AppliedAt)
                .ThenByDescending(application => application.Id)
                .Select(application => new { application.AppliedAt, application.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (newestApplication == null)
                return lastCompletedRefresh.HasValue ? null : key;
            if (!lastCompletedRefresh.HasValue || newestApplication.AppliedAt > lastCompletedRefresh.Value)
                return $"{key}:applications:{newestApplication.AppliedAt.Ticks}:{newestApplication.Id:D}";
            return null;
        }

        private async Task<bool> QueueJobRankingRefreshIfNeededAsync(
            Guid jobPostId,
            string key,
            CancellationToken cancellationToken)
        {
            var existing = await _context.OutboxMessages
                .AsNoTracking()
                .Where(message => message.DeduplicationKey == key)
                .Select(message => new { message.ProcessedAt, message.DeadLetteredAt })
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                return existing.ProcessedAt == null && existing.DeadLetteredAt == null;

            _outbox.Add("matching.job.refresh", new { JobPostId = jobPostId }, key);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsDuplicateDeduplicationKey(exception))
            {
                DetachPendingOutboxEntries();
            }
            return await HasPendingJobRefreshAsync(jobPostId, cancellationToken);
        }

        private void DetachPendingOutboxEntries()
        {
            foreach (var entry in _context.ChangeTracker.Entries<OutboxMessage>()
                         .Where(entry => entry.State == EntityState.Added))
                entry.State = EntityState.Detached;
        }

        private static bool IsDuplicateDeduplicationKey(DbUpdateException exception)
        {
            var message = exception.ToString();
            return message.Contains("DeduplicationKey", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
        }

        private Task<bool> HasPendingJobRefreshAsync(Guid jobPostId, CancellationToken cancellationToken)
            => _context.OutboxMessages.AnyAsync(message =>
                message.DeduplicationKey != null &&
                (message.DeduplicationKey == $"matching:job:{jobPostId:D}" ||
                 message.DeduplicationKey.StartsWith($"matching:job:{jobPostId:D}:")) &&
                message.ProcessedAt == null && message.DeadLetteredAt == null,
                cancellationToken);

        private static MatchResultDto ToDto(MatchResult result) => new()
        {
            JobPostId = result.JobPostId,
            CandidateId = result.CandidateId,
            TotalScore = result.TotalScore,
            AlgorithmVersion = result.AlgorithmVersion,
            Breakdown = JsonSerializer.Deserialize<MatchBreakdownDto>(result.BreakdownJson) ?? new MatchBreakdownDto(),
            MatchedSkills = JsonSerializer.Deserialize<List<string>>(result.MatchedSkillsJson) ?? new List<string>(),
            MissingSkills = JsonSerializer.Deserialize<List<string>>(result.MissingSkillsJson) ?? new List<string>(),
            Reason = JsonSerializer.Deserialize<List<string>>(result.ReasonsJson)?.FirstOrDefault() ?? string.Empty,
            InputFingerprint = result.InputFingerprint,
            JobPost = result.JobPost == null ? null : new MatchJobSummaryDto
            {
                Id = result.JobPost.Id,
                Title = result.JobPost.Title,
                Location = result.JobPost.Location,
                Salary = result.JobPost.Salary,
                Type = result.JobPost.Type,
                ExpiresAt = result.JobPost.ExpiresAt
            },
            Candidate = result.Candidate == null ? null : new MatchCandidateSummaryDto
            {
                Id = result.Candidate.Id,
                FullName = result.Candidate.FullName,
                Email = result.Candidate.Email
            }
        };

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
