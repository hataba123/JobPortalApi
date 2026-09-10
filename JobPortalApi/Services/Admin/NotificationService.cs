using JobPortalApi.DTOs.Notification;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.DTOs.Shared;

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

        public async Task<PagedResultDto<NotificationDto>> GetAllAsync(PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.Notifications.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(item => item.Message.Contains(search) || (item.Type != null && item.Type.Contains(search)));
            }
            var total = await query.CountAsync();
            var ordered = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(item => item.CreatedAt)
                : query.OrderByDescending(item => item.CreatedAt);
            var items = await ordered.ThenBy(item => item.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(n => new NotificationDto { Id = n.Id, UserId = n.UserId, Message = n.Message, CreatedAt = n.CreatedAt, Read = n.Read, Type = n.Type })
                .ToListAsync();
            return new PagedResultDto<NotificationDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
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

        public async Task<PagedResultDto<NotificationDto>> GetByUserIdAsync(Guid userId, PagedQuery request)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _context.Notifications.AsNoTracking().Where(item => item.UserId == userId);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(n => new NotificationDto { Id = n.Id, UserId = n.UserId, Message = n.Message, CreatedAt = n.CreatedAt, Read = n.Read, Type = n.Type })
                .ToListAsync();
            return new PagedResultDto<NotificationDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
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

        public async Task<NotificationDto?> GetByIdAsync(Guid id, Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.Id == id && n.UserId == userId)
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
                Type = dto.Type,
                SourceMessageId = dto.SourceMessageId
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

        public async Task<bool> MarkAsReadAsync(Guid id, Guid userId)
        {
            var updated = await _context.Notifications
                .Where(n => n.Id == id && n.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.Read, true));
            return updated == 1;
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var deleted = await _context.Notifications
                .Where(n => n.Id == id && n.UserId == userId)
                .ExecuteDeleteAsync();
            return deleted == 1;
        }

    }
}
