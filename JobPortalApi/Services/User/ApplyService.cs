
using JobPortalApi.DTOs.Apply;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class ApplyService : IApplyService
    {
        private readonly ApplicationDbContext _context;

        public ApplyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ApplyToJobAsync(Guid candidateId, JobApplicationRequest request)
        {
            var jobPost = await _context.JobPosts.FindAsync(request.JobPostId);
            if (jobPost == null)
                throw new Exception("Công việc không tồn tại.");

            // Tìm hồ sơ ứng viên
            var candidateProfile = await _context.candidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == candidateId);
            if (candidateProfile == null)
                throw new Exception("Hồ sơ ứng viên chưa tồn tại.");

            // Nếu CV không được truyền từ request => dùng CV trong profile nếu có
            var cvUrl = request.CVUrl;
            if (string.IsNullOrEmpty(cvUrl))
                cvUrl = candidateProfile.ResumeUrl;

            if (string.IsNullOrEmpty(cvUrl))
                throw new Exception("Bạn cần tải lên CV trước khi ứng tuyển.");

            var apply = new Job
            {
                Id = Guid.NewGuid(),
                JobPostId = request.JobPostId,
                CandidateId = candidateId,
                CVUrl = cvUrl,
                AppliedAt = DateTime.UtcNow,
                Status = ApplyStatus.Pending
            };

            _context.Jobs.Add(apply);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Bạn đã ứng tuyển công việc này rồi.");
            }
        }


        public async Task<List<CandidateApplicationDto>> GetCandidatesAppliedToJob(Guid employerId, Guid jobPostId)
        {
            var jobPost = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == jobPostId && j.EmployerId == employerId);
            if (jobPost == null)
                throw new Exception("Không tìm thấy công việc hoặc bạn không có quyền.");

            return await _context.Jobs
                .Where(a => a.JobPostId == jobPostId)
                .Include(a => a.Candidate)
                    .Select(a => new CandidateApplicationDto
                    {
                        Id = a.Id, // ✅ cần gán ở đây
                        CandidateId = a.CandidateId,
                        FullName = a.Candidate.FullName,
                        Email = a.Candidate.Email,
                        AppliedAt = a.AppliedAt,
                        CVUrl = a.CVUrl,
                        Status = a.Status // ✅ THÊM DÒNG NÀY

                    })
                .ToListAsync();
        }

        public async Task<List<JobAppliedDto>> GetJobsAppliedByCandidate(Guid candidateId)
        {
            return await _context.Jobs
                .Where(a => a.CandidateId == candidateId)
                .Include(a => a.JobPost)
                .Select(a => new JobAppliedDto
                {
                    Id = a.Id,
                    JobPostId = a.JobPostId,
                    Title = a.JobPost.Title,
                    Location = a.JobPost.Location,
                    Salary = a.JobPost.Salary,
                    SkillsRequired = a.JobPost.SkillsRequired,
                    Description = a.JobPost.Description,
                    AppliedAt = a.AppliedAt,
                    Status = a.Status // ✅ THÊM DÒNG NÀY

                })
                .ToListAsync();
        }

        public async Task<List<ApplyDto>> GetAllAsync()
        {
            return await _context.Jobs
                .Include(j => j.JobPost)
                .Include(j => j.Candidate)
                .Select(j => new ApplyDto
                {
                    Id = j.Id,
                    CandidateId = j.CandidateId,
                    CandidateName = j.Candidate.FullName,
                    JobPostId = j.JobPostId,
                    JobTitle = j.JobPost.Title,
                    CVUrl = j.CVUrl,
                    Status = j.Status.ToString(),
                    AppliedAt = j.AppliedAt
                })
                .ToListAsync();
        }

        public async Task<ApplyDto?> GetByIdAsync(Guid id)
        {
            return await _context.Jobs
                .Include(j => j.JobPost)
                .Include(j => j.Candidate)
                .Where(j => j.Id == id)
                .Select(j => new ApplyDto
                {
                    Id = j.Id,
                    CandidateId = j.CandidateId,
                    CandidateName = j.Candidate.FullName,
                    JobPostId = j.JobPostId,
                    JobTitle = j.JobPost.Title,
                    CVUrl = j.CVUrl,
                    Status = j.Status.ToString(),
                    AppliedAt = j.AppliedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ApplyDto?> GetByIdForUserAsync(Guid id, Guid userId, bool isAdmin)
        {
            IQueryable<Job> query = _context.Jobs
                .Include(j => j.JobPost)
                .Include(j => j.Candidate)
                .Where(j => j.Id == id);
            if (!isAdmin)
                query = query.Where(j => j.CandidateId == userId);

            return await query.Select(j => new ApplyDto
            {
                Id = j.Id,
                CandidateId = j.CandidateId,
                CandidateName = j.Candidate.FullName,
                JobPostId = j.JobPostId,
                JobTitle = j.JobPost.Title,
                CVUrl = j.CVUrl,
                Status = j.Status.ToString(),
                AppliedAt = j.AppliedAt
            }).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateStatusAsync(Guid id, string status, Guid actorId, bool isAdmin)
        {
            var apply = await _context.Jobs
                .Include(j => j.JobPost)
                .FirstOrDefaultAsync(j => j.Id == id);
            if (apply == null) return false;
            if (!isAdmin && apply.JobPost.EmployerId != actorId) return false;

            if (!Enum.TryParse<ApplyStatus>(status, out var newStatus))
                throw new Exception("Trạng thái không hợp lệ.");

            apply.Status = newStatus;
            _context.Jobs.Update(apply);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var apply = await _context.Jobs.FindAsync(id);
            if (apply == null) return false;

            _context.Jobs.Remove(apply);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
