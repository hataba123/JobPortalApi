using JobPortalApi.DTOs.Notification;
using JobPortalApi.Services.Interface.Admin;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize] // ✅ Người dùng đã đăng nhập
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // 🔹 GET: api/notifications
        // 👉 Lấy danh sách thông báo của user hiện tại
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _notificationService.GetByUserIdAsync(userId);
            return Ok(result);
        }

        // 🔹 GET: api/notifications/{id}
        // 👉 Xem chi tiết thông báo
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _notificationService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // 🔹 PUT: api/notifications/{id}/read
        // 👉 Đánh dấu là đã đọc
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            return success ? Ok(new { message = "Thông báo đã được đánh dấu là đã đọc." }) : NotFound();
        }

        // 🔹 DELETE: api/notifications/{id}
        // 👉 Xoá thông báo
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _notificationService.DeleteAsync(id);
            return success ? Ok(new { message = "Xoá thông báo thành công." }) : NotFound();
        }
    }
}
