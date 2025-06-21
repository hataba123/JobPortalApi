using JobPortalApi.DTOs.Notification;

namespace JobPortalApi.Services.Interface.Admin
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetAllAsync();
        Task<NotificationDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId);
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> DeleteAsync(Guid id);
    }
}
