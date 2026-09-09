using System.Net.Http.Json;

namespace JobPortalApi.Services.Notifications
{
    public sealed class EmailNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EmailNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public Task SendApplicationConfirmationAsync(string to, string jobTitle) => SendAsync(
            to,
            $"Đã nhận hồ sơ ứng tuyển: {jobTitle}",
            $"Hồ sơ của bạn cho tin \"{jobTitle}\" đã được hệ thống tiếp nhận.");

        public Task SendApplicationStatusChangedAsync(string to, string jobTitle, string status) => SendAsync(
            to,
            $"Cập nhật hồ sơ ứng tuyển: {jobTitle}",
            $"Trạng thái hồ sơ cho tin \"{jobTitle}\" đã chuyển thành {status}.");

        public Task SendPasswordResetAsync(string to, string token)
        {
            var frontendUrl = (_configuration["Frontend:BaseUrl"]
                ?? Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? "http://localhost:3000").TrimEnd('/');
            var resetUrl = $"{frontendUrl}/vi/candidate/auth/reset-password?email={Uri.EscapeDataString(to)}&token={Uri.EscapeDataString(token)}";
            return SendAsync(
                to,
                "Đặt lại mật khẩu JobPortal",
                $"Mở liên kết sau để đặt lại mật khẩu (liên kết hết hạn sau 15 phút): {resetUrl}");
        }

        private async Task SendAsync(string to, string subject, string text)
        {
            var webhookUrl = _configuration["Email:WebhookUrl"]
                ?? Environment.GetEnvironmentVariable("EMAIL_WEBHOOK_URL");
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
                {
                    Content = JsonContent.Create(new { to, subject, text })
                };
                var secret = _configuration["Email:WebhookSecret"]
                    ?? Environment.GetEnvironmentVariable("EMAIL_WEBHOOK_SECRET");
                if (!string.IsNullOrWhiteSpace(secret))
                    request.Headers.Authorization = new("Bearer", secret);

                using var response = await _httpClientFactory
                    .CreateClient("email-provider")
                    .SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("Email provider trả về HTTP {StatusCode}.", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                // Email không được làm thất bại nghiệp vụ ứng tuyển hoặc reset mật khẩu.
                _logger.LogWarning(ex, "Không thể gửi email qua provider.");
            }
        }
    }
}
