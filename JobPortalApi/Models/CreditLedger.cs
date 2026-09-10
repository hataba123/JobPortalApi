using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.Models;

[Table("CreditLedgers")]
public class CreditLedger
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public Guid? PaymentOrderId { get; set; }

    public CreditType CreditType { get; set; }

    public CreditLedgerEntryType EntryType { get; set; } = CreditLedgerEntryType.Grant;

    public int Quantity { get; set; }

    [MaxLength(200)]
    public string? IdempotencyKey { get; set; }

    [MaxLength(200)]
    public string? SourceIdempotencyKey { get; set; }

    public Guid? ActorId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(PaymentOrderId))]
    public PaymentOrder? PaymentOrder { get; set; }
}
