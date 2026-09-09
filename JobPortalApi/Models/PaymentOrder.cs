using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.Models;

[Table("PaymentOrders")]
public class PaymentOrder
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    [Required, MaxLength(64)]
    public string VnpTxnRef { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Pending;

    [MaxLength(10)]
    public string? ProviderResponseCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(PlanId))]
    public ServicePlan Plan { get; set; } = null!;

    public ICollection<CreditLedger> CreditLedger { get; set; } = new List<CreditLedger>();
}
