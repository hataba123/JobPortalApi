using JobPortalApi.DTOs.Company;

namespace JobPortalApi.Services.Interface.User
{
    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllAsync();
        Task<CompanyDto?> GetByIdAsync(Guid id);
    }
}

