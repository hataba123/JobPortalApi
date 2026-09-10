using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("AuditLogs")]
public sealed class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid? ActorId { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    public string? Before { get; set; }
    public string? After { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(64)]
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }
}
