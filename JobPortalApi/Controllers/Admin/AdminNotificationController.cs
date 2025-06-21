using JobPortalApi.DTOs.Notification;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // 🔹 Get all notifications
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _notificationService.GetAllAsync();
            return Ok(result);
        }

        // 🔹 Get notification by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _notificationService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // 🔹 Create new notification
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            var result = await _notificationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // 🔹 Mark as read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            return success ? Ok(new { message = "Đã đánh dấu là đã đọc." }) : NotFound();
        }

        // 🔹 Delete notification
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _notificationService.DeleteAsync(id);
            return success ? Ok(new { message = "Xoá thông báo thành công." }) : NotFound();
        }
    }
}
