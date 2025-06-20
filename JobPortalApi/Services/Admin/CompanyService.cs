using JobPortalApi.DTOs.AdminCompany;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Companies
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
                    Tags = c.Tags
                })
                .ToListAsync();
        }

        public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id)
        {
            var c = await _context.Companies.FindAsync(id);
            if (c == null) return null;
            return new CompanyDto
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
                Tags = c.Tags
            };
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

        public async Task<bool> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto)
        {
            var c = await _context.Companies.FindAsync(id);
            if (c == null) return false;
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
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCompanyAsync(Guid id)
        {
            var c = await _context.Companies.FindAsync(id);
            if (c == null) return false;
            _context.Companies.Remove(c);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
