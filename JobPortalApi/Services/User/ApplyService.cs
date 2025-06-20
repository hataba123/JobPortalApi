
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

            var alreadyApplied = await _context.Jobs
                .AnyAsync(a => a.JobPostId == request.JobPostId && a.CandidateId == candidateId);
            if (alreadyApplied)
                throw new Exception("Bạn đã ứng tuyển công việc này rồi.");

            var apply = new Job
            {
                Id = Guid.NewGuid(),
                JobPostId = request.JobPostId,
                CandidateId = candidateId,
                CVUrl = request.CVUrl,
                AppliedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(apply);
            await _context.SaveChangesAsync();
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
                        CandidateId = a.CandidateId,
                        FullName = a.Candidate.FullName,
                        Email = a.Candidate.Email,
                        AppliedAt = a.AppliedAt,
                        CVUrl = a.CVUrl
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
                    JobPostId = a.JobPostId,
                    Title = a.JobPost.Title,
                    Location = a.JobPost.Location,
                    Salary = a.JobPost.Salary,
                    SkillsRequired = a.JobPost.SkillsRequired,
                    Description = a.JobPost.Description,
                    AppliedAt = a.AppliedAt
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

        public async Task<bool> UpdateStatusAsync(Guid id, string status)
        {
            var apply = await _context.Jobs.FindAsync(id);
            if (apply == null) return false;

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
