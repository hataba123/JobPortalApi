using System.Security.Claims;

namespace JobPortalApi.Services.Infrastructure;

public interface IRequestContext
{
    Guid? ActorId { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }
}

public sealed class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? ActorId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? CorrelationId => _accessor.HttpContext?.TraceIdentifier;
}
