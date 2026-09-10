using JobPortalApi.DTOs.AdminCompany;

namespace JobPortalApi.Services.Interface.User
{
    public interface IRecruiterCompanyService
    {
        Task<CompanyDto?> GetMyCompanyAsync(Guid employerId);
        Task<bool> UpdateMyCompanyAsync(Guid employerId, UpdateCompanyDto dto, byte[]? expectedVersion = null);
        Task<bool> DeleteMyCompanyAsync(Guid employerId, byte[]? expectedVersion = null);

    }
}
