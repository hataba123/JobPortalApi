namespace JobPortalApi.DTOs.Notification
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool Read { get; set; }             // 🔄 Đổi từ IsRead → Read
        public string? Type { get; set; }
    }
}
