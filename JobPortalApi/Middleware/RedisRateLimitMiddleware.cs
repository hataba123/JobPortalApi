using StackExchange.Redis;
using System.Security.Claims;

namespace JobPortalApi.Middleware;

/// <summary>
/// Giới hạn theo IP/người dùng dùng chung giữa các instance API.
/// Rate limiter tích hợp của ASP.NET vẫn được giữ làm lớp dự phòng cho các endpoint
/// đăng nhập và thanh toán khi Redis không sẵn sàng.
/// </summary>
public sealed class RedisRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RedisRateLimitMiddleware> _logger;
    private readonly IConnectionMultiplexer? _redis;

    public RedisRateLimitMiddleware(
        RequestDelegate next,
        ILogger<RedisRateLimitMiddleware> logger,
        IServiceProvider services)
    {
        _next = next;
        _logger = logger;
        _redis = services.GetService<IConnectionMultiplexer>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_redis == null || !TryGetPolicy(context.Request.Path, out var policy))
        {
            await _next(context);
            return;
        }

        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clientAddress = string.IsNullOrWhiteSpace(subject)
            ? $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"
            : $"user:{subject}";
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        var key = $"jobportal:rate:{policy.Name}:{clientAddress}:{bucket}";

        try
        {
            var database = _redis.GetDatabase();
            var count = await database.StringIncrementAsync(key);
            if (count == 1)
                await database.KeyExpireAsync(key, TimeSpan.FromMinutes(2));

            if (count > policy.Limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = "60";
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = StatusCodes.Status429TooManyRequests,
                    code = "RATE_LIMITED",
                    message = "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau."
                });
                return;
            }
        }
        catch (RedisException exception)
        {
            // Khi Redis tạm thời lỗi, lớp fixed-window tại ASP.NET vẫn bảo vệ từng instance.
            // Không ghi địa chỉ IP hoặc dữ liệu request vào log.
            _logger.LogWarning(exception, "Redis rate limiter unavailable; using local limiter");
        }

        await _next(context);
    }

    private static bool TryGetPolicy(PathString path, out RateLimitPolicy policy)
    {
        var value = path.Value ?? string.Empty;
        if (value.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/ready", StringComparison.OrdinalIgnoreCase))
        {
            policy = default;
            return false;
        }

        if (value.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            policy = new RateLimitPolicy("auth", 30);
            return true;
        }

        if (value.Equals("/api/payments/vnpay/ipn", StringComparison.OrdinalIgnoreCase))
        {
            policy = new RateLimitPolicy("payment-ipn", 120);
            return true;
        }

        if (value.StartsWith("/api/payment-orders", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/api/credits/", StringComparison.OrdinalIgnoreCase))
        {
            policy = new RateLimitPolicy("payment", 60);
            return true;
        }

        if (value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            // Hạn mức riêng cho API người dùng, tách khỏi đăng nhập và thanh toán.
            policy = new RateLimitPolicy("api", 300);
            return true;
        }

        policy = default;
        return false;
    }

    private readonly record struct RateLimitPolicy(string Name, int Limit);
}
