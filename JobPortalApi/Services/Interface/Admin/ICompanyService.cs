using JobPortalApi.DTOs.AdminCompany;

using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface ICompanyService
    {
        Task<List<CompanyDto>> GetAllCompaniesAsync();
        Task<PagedResultDto<CompanyDto>> GetAllCompaniesAsync(PagedQuery query);
        Task<CompanyDto?> GetCompanyByIdAsync(Guid id);
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto);
        Task<bool> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto, byte[]? expectedVersion = null);
        Task<bool> DeleteCompanyAsync(Guid id, byte[]? expectedVersion = null);
        Task<CompanyDto?> UpdateVerificationStatusAsync(Guid id, UpdateCompanyVerificationDto dto, byte[]? expectedVersion = null);
    }
}
