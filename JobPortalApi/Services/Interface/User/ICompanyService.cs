using JobPortalApi.DTOs.Company;

using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.User
{
    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllAsync();
        Task<PagedResponse<CompanyDto>> GetAllPagedAsync(
            PagedQuery query,
            string? industry = null,
            string? location = null,
            string? employees = null);
        Task<CompanyDto?> GetByIdAsync(Guid id);
    }
}

