using JobPortalApi.DTOs.AdminCompany;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.User
{
    public class RecruiterCompanyService : IRecruiterCompanyService
    {
        private readonly ApplicationDbContext _context;

        public RecruiterCompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyDto?> GetMyCompanyAsync(Guid employerId)
        {
            // Tìm công ty đầu tiên mà recruiter đã từng đăng bài
            var company = await _context.JobPosts
                .Where(j => j.EmployerId == employerId && j.CompanyId != null)
                .Select(j => j.Company)
                .Distinct()
                .FirstOrDefaultAsync();

            if (company == null) return null;

            return new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Logo = company.Logo,
                Description = company.Description,
                Location = company.Location,
                Employees = company.Employees,
                Industry = company.Industry,
                OpenJobs = company.OpenJobs,
                Rating = company.Rating,
                Website = company.Website,
                Founded = company.Founded,
                Tags = company.Tags
            };
        }

        public async Task<bool> UpdateMyCompanyAsync(Guid employerId, UpdateCompanyDto dto)
        {
            var company = await _context.JobPosts
                .Where(j => j.EmployerId == employerId && j.CompanyId != null)
                .Select(j => j.Company)
                .Distinct()
                .FirstOrDefaultAsync();

            if (company == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.Name)) company.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Logo)) company.Logo = dto.Logo;
            if (!string.IsNullOrWhiteSpace(dto.Description)) company.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.Location)) company.Location = dto.Location;
            if (!string.IsNullOrWhiteSpace(dto.Employees)) company.Employees = dto.Employees;
            if (!string.IsNullOrWhiteSpace(dto.Industry)) company.Industry = dto.Industry;
            if (dto.OpenJobs.HasValue) company.OpenJobs = dto.OpenJobs.Value;
            if (dto.Rating.HasValue) company.Rating = dto.Rating.Value;
            if (!string.IsNullOrWhiteSpace(dto.Website)) company.Website = dto.Website;
            if (!string.IsNullOrWhiteSpace(dto.Founded)) company.Founded = dto.Founded;
            if (!string.IsNullOrWhiteSpace(dto.Tags)) company.Tags = dto.Tags;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteMyCompanyAsync(Guid employerId)
        {
            var company = await _context.JobPosts
                .Where(j => j.EmployerId == employerId && j.CompanyId != null)
                .Select(j => j.Company)
                .Distinct()
                .FirstOrDefaultAsync();

            if (company == null) return false;

            var hasJobs = await _context.JobPosts.AnyAsync(j => j.CompanyId == company.Id);
            if (hasJobs) return false;

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
