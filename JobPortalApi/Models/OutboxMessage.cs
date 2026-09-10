using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("OutboxMessages")]
public sealed class OutboxMessage
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }

    public DateTime? DeadLetteredAt { get; set; }

    [MaxLength(200)]
    public string? DeduplicationKey { get; set; }

    [MaxLength(64)]
    public string? CorrelationId { get; set; }
}
