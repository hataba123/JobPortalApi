using JobPortalApi.DTOs.RecruiterDashboard;

namespace JobPortalApi.Services.Interface.User
{
    public interface IRecruiterDashboardService
    {
        Task<RecruiterDashboardDto> GetDashboardAsync(Guid recruiterId);

    }
}
