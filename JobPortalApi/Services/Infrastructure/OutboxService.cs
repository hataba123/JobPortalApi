using System.Text.Json;
using JobPortalApi.Models;

namespace JobPortalApi.Services.Infrastructure;

public interface IOutboxService
{
    void Add<T>(string type, T payload, string? deduplicationKey = null);
}

public sealed class OutboxService : IOutboxService
{
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
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = DateTime.UtcNow,
            NextAttemptAt = DateTime.UtcNow,
            DeduplicationKey = deduplicationKey,
            CorrelationId = _requestContext.CorrelationId
        });
    }
}
