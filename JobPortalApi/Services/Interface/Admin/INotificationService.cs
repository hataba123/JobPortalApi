using JobPortalApi.DTOs.Notification;

using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetAllAsync();
        Task<PagedResultDto<NotificationDto>> GetAllAsync(PagedQuery query);
        Task<PagedResultDto<NotificationDto>> GetByUserIdAsync(Guid userId, PagedQuery query);
        Task<NotificationDto?> GetByIdAsync(Guid id);
        Task<NotificationDto?> GetByIdAsync(Guid id, Guid userId);
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId);
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAsReadAsync(Guid id, Guid userId);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> DeleteAsync(Guid id, Guid userId);
    }
}
