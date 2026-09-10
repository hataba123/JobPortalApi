namespace JobPortalApi.DTOs.Audit;

public sealed class AuditLogDto
{
    public Guid Id { get; init; }
    public Guid? ActorId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string? Before { get; init; }
    public string? After { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime CreatedAt { get; init; }
}
