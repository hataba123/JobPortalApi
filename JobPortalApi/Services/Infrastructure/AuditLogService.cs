using System.Text.Json;
using System.Text.Json.Nodes;
using JobPortalApi.Models;

namespace JobPortalApi.Services.Infrastructure;

public interface IAuditLogService
{
    void Add(string action, string entityType, string entityId, object? before, object? after);
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IRequestContext _requestContext;

    public AuditLogService(ApplicationDbContext context, IRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public void Add(string action, string entityType, string entityId, object? before, object? after)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = _requestContext.ActorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = Sanitize(before),
            After = Sanitize(after),
            IpAddress = _requestContext.IpAddress,
            CorrelationId = _requestContext.CorrelationId,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string? Sanitize(object? value)
    {
        if (value == null) return null;
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyProperties = true
        });
        var node = JsonNode.Parse(json);
        RemoveSensitiveFields(node);
        var sanitized = node?.ToJsonString();
        return sanitized is { Length: > 100_000 }
            ? sanitized[..100_000]
            : sanitized;
    }

    private static void RemoveSensitiveFields(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (IsSensitive(property.Key))
                {
                    obj.Remove(property.Key);
                    continue;
                }
                RemoveSensitiveFields(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) RemoveSensitiveFields(item);
        }
    }

    private static bool IsSensitive(string name)
    {
        var key = name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        return key.Contains("password") || key.Contains("token") || key.Contains("secret") ||
               key.Contains("signature") || key.Contains("payment") || key.Contains("cv") ||
               key.Contains("resume") || key.Contains("storagepath") || key == "path";
    }
}
