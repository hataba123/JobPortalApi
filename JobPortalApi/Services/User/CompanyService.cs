using JobPortalApi.DTOs.Company;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Infrastructure;

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
            var companies = await _context.Companies
                .Where(c => c.VerificationStatus == CompanyVerificationStatus.Verified)
                .ToListAsync();
            return companies.Select(ToDto).ToList();
        }

        public async Task<CompanyDto?> GetByIdAsync(Guid id)
        {
            var company = await _context.Companies
                .Where(c => c.Id == id)
                .Where(c => c.VerificationStatus == CompanyVerificationStatus.Verified)
                .FirstOrDefaultAsync();
            return company == null ? null : ToDto(company);
        }

        private static CompanyDto ToDto(Company c) => new()
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
            VerifiedAt = c.VerifiedAt,
            Version = ConcurrencyToken.Encode(c.RowVersion)
        };
    }
}
