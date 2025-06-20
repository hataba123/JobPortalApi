using JobPortalApi.DTOs.AdminDashboard;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardStatsAsync();
    }
}
