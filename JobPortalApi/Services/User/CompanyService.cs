using JobPortalApi.DTOs.Company;
using JobPortalApi.Services.Interface.User;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.DTOs.Shared;

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

        public async Task<PagedResponse<CompanyDto>> GetAllPagedAsync(
            PagedQuery request,
            string? industry = null,
            string? location = null,
            string? employees = null)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.Companies
                .AsNoTracking()
                .Where(company => company.VerificationStatus == CompanyVerificationStatus.Verified);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(company => company.Name.Contains(search) ||
                    (company.Location != null && company.Location.Contains(search)) ||
                    (company.Industry != null && company.Industry.Contains(search)));
            }
            if (!string.IsNullOrWhiteSpace(industry))
                query = query.Where(company => company.Industry == industry.Trim());
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(company => company.Location == location.Trim());
            if (!string.IsNullOrWhiteSpace(employees))
                query = query.Where(company => company.Employees == employees.Trim());

            var total = await query.CountAsync();
            var companies = await query
                .OrderBy(company => company.Name)
                .ThenBy(company => company.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<CompanyDto>
            {
                Items = companies.Select(ToDto).ToList(),
                TotalCount = total,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            };
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
