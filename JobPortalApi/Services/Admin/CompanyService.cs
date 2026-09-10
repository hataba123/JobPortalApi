using JobPortalApi.DTOs.AdminCompany;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Middleware;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Admin
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CompanyDto>> GetAllCompaniesAsync()
        {
            var companies = await _context.Companies.AsNoTracking().ToListAsync();
            return companies.Select(ToDto).ToList();
        }

        public async Task<PagedResultDto<CompanyDto>> GetAllCompaniesAsync(PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.Companies.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(company => company.Name.Contains(search) ||
                    (company.Location != null && company.Location.Contains(search)) ||
                    (company.Industry != null && company.Industry.Contains(search)));
            }
            var total = await query.CountAsync();
            var ordered = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
                ? request.SortBy?.ToLowerInvariant() switch
                {
                    "name" => query.OrderBy(company => company.Name),
                    "rating" => query.OrderBy(company => company.Rating),
                    _ => query.OrderBy(company => company.Id)
                }
                : request.SortBy?.ToLowerInvariant() switch
                {
                    "name" => query.OrderByDescending(company => company.Name),
                    "rating" => query.OrderByDescending(company => company.Rating),
                    _ => query.OrderByDescending(company => company.Id)
                };
            var entities = await ordered.ThenBy(company => company.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResultDto<CompanyDto>
            {
                Items = entities.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize
            };
        }

        public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id)
        {
            var c = await _context.Companies.FindAsync(id);
            if (c == null) return null;
            return ToDto(c);
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto)
        {
            var c = new Company
            {
                Name = dto.Name,
                Logo = dto.Logo,
                Description = dto.Description,
                Location = dto.Location,
                Employees = dto.Employees,
                Industry = dto.Industry,
                OpenJobs = dto.OpenJobs,
                Rating = dto.Rating,
                Website = dto.Website,
                Founded = dto.Founded,
                Tags = dto.Tags
            };
            _context.Companies.Add(c);
            await _context.SaveChangesAsync();
            return await GetCompanyByIdAsync(c.Id);
        }

        public async Task<bool> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto, byte[]? expectedVersion = null)
        {
            var c = await _context.Companies.FindAsync(id);
            if (c == null) return false;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(c).Property(company => company.RowVersion).OriginalValue = expectedVersion;
            if (!string.IsNullOrWhiteSpace(dto.Name)) c.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Logo)) c.Logo = dto.Logo;
            if (!string.IsNullOrWhiteSpace(dto.Description)) c.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.Location)) c.Location = dto.Location;
            if (!string.IsNullOrWhiteSpace(dto.Employees)) c.Employees = dto.Employees;
            if (!string.IsNullOrWhiteSpace(dto.Industry)) c.Industry = dto.Industry;
            if (dto.OpenJobs.HasValue) c.OpenJobs = dto.OpenJobs.Value;
            if (dto.Rating.HasValue) c.Rating = dto.Rating.Value;
            if (!string.IsNullOrWhiteSpace(dto.Website)) c.Website = dto.Website;
            if (!string.IsNullOrWhiteSpace(dto.Founded)) c.Founded = dto.Founded;
            if (!string.IsNullOrWhiteSpace(dto.Tags)) c.Tags = dto.Tags;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Công ty đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return true;
        }

        public async Task<bool> DeleteCompanyAsync(Guid id, byte[]? expectedVersion = null)
        {
            var c = await _context.Companies.FindAsync(id);
            if (c == null) return false;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(c).Property(company => company.RowVersion).OriginalValue = expectedVersion;
            c.DeletedAt = DateTime.UtcNow;
            var linkedPosts = await _context.JobPosts
                .Where(j => j.CompanyId == id)
                .ToListAsync();
            foreach (var post in linkedPosts)
            {
                post.DeletedAt = c.DeletedAt;
                post.Status = JobPostStatus.Closed;
            }
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Công ty đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return true;
        }

        public async Task<CompanyDto?> UpdateVerificationStatusAsync(Guid id, UpdateCompanyVerificationDto dto, byte[]? expectedVersion = null)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null) return null;
            if (expectedVersion is { Length: > 0 })
                _context.Entry(company).Property(item => item.RowVersion).OriginalValue = expectedVersion;

            company.VerificationStatus = dto.VerificationStatus;
            company.VerifiedAt = dto.VerificationStatus == CompanyVerificationStatus.Verified
                ? DateTime.UtcNow
                : null;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                throw new ApiConflictException("Công ty đã được cập nhật bởi người khác. Vui lòng tải lại.", "CONCURRENCY_CONFLICT");
            }
            return await GetCompanyByIdAsync(id);
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
