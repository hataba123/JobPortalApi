using JobPortalApi.DTOs.Company;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.Services.User

{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CompanyDto>> GetAllAsync()
        {
            return await _context.Companies
                .Where(c => c.VerificationStatus == CompanyVerificationStatus.Verified)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Logo = c.Logo,
                    Description = c.Description,
                    Location = c.Location,
                    Employees = c.Employees,
                    Industry = c.Industry,
                    OpenJobs = c.OpenJobs,
                    Rating = c.Rating,
                    Website = c.Website,
                    Founded = c.Founded,
                    Tags = c.Tags,
                    VerificationStatus = c.VerificationStatus,
                    VerifiedAt = c.VerifiedAt
                })
                .ToListAsync();
        }

        public async Task<CompanyDto?> GetByIdAsync(Guid id)
        {
            return await _context.Companies
                .Where(c => c.Id == id)
                .Where(c => c.VerificationStatus == CompanyVerificationStatus.Verified)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Logo = c.Logo,
                    Description = c.Description,
                    Location = c.Location,
                    Employees = c.Employees,
                    Industry = c.Industry,
                    OpenJobs = c.OpenJobs,
                    Rating = c.Rating,
                    Website = c.Website,
                    Founded = c.Founded,
                    Tags = c.Tags,
                    VerificationStatus = c.VerificationStatus,
                    VerifiedAt = c.VerifiedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}
