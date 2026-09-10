using System.Text.Json;
using System.Text.Json.Serialization;
using JobPortalApi.Models;

namespace JobPortalApi.Services.Infrastructure;

public interface IOutboxService
{
    void Add<T>(string type, T payload, string? deduplicationKey = null);
}

public sealed class OutboxService : IOutboxService
{
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApplicationDbContext _context;
    private readonly IRequestContext _requestContext;

    public OutboxService(ApplicationDbContext context, IRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public void Add<T>(string type, T payload, string? deduplicationKey = null)
    {
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            // Payloads are a versioned contract. Serialize enums by name so
            // workers and the equivalent NestJS backend do not depend on the
            // numeric order of an enum.
            Payload = JsonSerializer.Serialize(payload, PayloadOptions),
            OccurredAt = DateTime.UtcNow,
            NextAttemptAt = DateTime.UtcNow,
            DeduplicationKey = deduplicationKey,
            CorrelationId = _requestContext.CorrelationId
        });
    }
}
