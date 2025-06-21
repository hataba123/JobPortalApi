using JobPortalApi.DTOs.Notification;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Admin
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationDto>> GetAllAsync()
        {
            return await _context.Notifications
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    Read = n.Read,           // 🔄 Sửa ở đây
                    Type = n.Type
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    Read = n.Read, // hoặc n.IsRead nếu bạn đã sửa DTO
                                     // Nếu có thêm field Type
                                     // Type = n.Type 
                })
                .ToListAsync();
        }
        public async Task<NotificationDto?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications
                .Where(n => n.Id == id)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    Read = n.Read,
                    Type = n.Type
                })
                .FirstOrDefaultAsync();
        }

        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow,
                Read = false,
                Type = dto.Type
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                Read = notification.Read,
                Type = notification.Type
            };
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            notification.Read = true;              // 🔄 Sửa tên field
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
