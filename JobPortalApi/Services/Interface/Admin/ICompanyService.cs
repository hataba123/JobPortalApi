using JobPortalApi.DTOs.AdminCompany;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface ICompanyService
    {
        Task<List<CompanyDto>> GetAllCompaniesAsync();
        Task<CompanyDto?> GetCompanyByIdAsync(Guid id);
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto);
        Task<bool> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto);
        Task<bool> DeleteCompanyAsync(Guid id);
    }
}
