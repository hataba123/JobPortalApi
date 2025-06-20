using JobPortalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortalApi.Controllers.Candidate
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Recruiter")]
    public class EmployerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployerController(ApplicationDbContext context)
        {
            _context = context;
        }
        // mo rong 4
        // GET: api/employer/my-jobs
        [HttpGet("my-jobs")]
        public async Task<IActionResult> GetMyPostedJobs()
        {
            // Lấy userId từ token
            var employerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(employerId))
                return Unauthorized("Không tìm thấy thông tin nhà tuyển dụng.");

            var jobs = await _context.JobPosts
                .Where(j => j.EmployerId.ToString() == employerId)
                .Select(j => new
                {
                    j.Id,
                    j.Title,
                    j.Description,
                    j.Location,
                    j.Salary,
                    j.SkillsRequired
                })
                .ToListAsync();

            return Ok(jobs);
        }
        // mo rong 5: chinh sua job da dang
        // PUT: api/employer/my-jobs/{id}
        [HttpPut("my-jobs/{id}")]
        public async Task<IActionResult> UpdateJob(Guid id, [FromBody] JobPost updatedJob)
        {
            //Chỉ Employer đã đăng job mới được phép sửa hoặc xoá.            
            // Ta đã kiểm tra job.EmployerId == userId.
            var employerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (employerId == null)
                return Unauthorized();

            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == id && j.EmployerId.ToString() == employerId);
            if (job == null)
                return NotFound("Không tìm thấy job hoặc bạn không có quyền sửa.");

            // Cập nhật các thuộc tính
            job.Title = updatedJob.Title;
            job.Description = updatedJob.Description;
            job.Location = updatedJob.Location;
            job.Salary = updatedJob.Salary;
            job.SkillsRequired = updatedJob.SkillsRequired;

            await _context.SaveChangesAsync();
            return Ok("Cập nhật job thành công.");
        }
        // xoa job da dang
        // DELETE: api/employer/my-jobs/{id}
        [HttpDelete("my-jobs/{id}")]
        public async Task<IActionResult> DeleteJob(Guid id)
        {
            var employerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (employerId == null)
                return Unauthorized();

            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == id && j.EmployerId.ToString() == employerId);
            if (job == null)
                return NotFound("Không tìm thấy job hoặc bạn không có quyền xoá.");

            _context.JobPosts.Remove(job);
            await _context.SaveChangesAsync();
            return Ok("Xoá job thành công.");
        }

    }
}
