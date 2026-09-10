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

    [MaxLength(64)]
    public string? VnpTransactionNo { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // Giữ nguyên thông tin gói tại thời điểm tạo đơn; IPN không được tính lại
    // entitlement từ ServicePlan hiện tại sau khi admin chỉnh sửa gói.
    [MaxLength(120)]
    public string? PlanNameSnapshot { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PriceSnapshot { get; set; }

    public string? EntitlementsSnapshot { get; set; }

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
