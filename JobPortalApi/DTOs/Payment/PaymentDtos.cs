using System.ComponentModel.DataAnnotations;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.DTOs.Payment;

public class PlanEntitlementRequest
{
    public CreditType CreditType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(1, int.MaxValue)]
    public int? ExpiresInDays { get; set; }
}

public class CreateServicePlanRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    [Required, MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public bool IsActive { get; set; } = true;

    [Required, MinLength(1)]
    public List<PlanEntitlementRequest> Entitlements { get; set; } = new();
}

public class UpdateServicePlanRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    [Required, MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public bool IsActive { get; set; }

    [Required, MinLength(1)]
    public List<PlanEntitlementRequest> Entitlements { get; set; } = new();
}

public class CreatePaymentOrderRequest
{
    [Required]
    public Guid PlanId { get; set; }
}

public class PlanEntitlementDto
{
    public CreditType CreditType { get; set; }
    public int Quantity { get; set; }
    public int? ExpiresInDays { get; set; }
}

public class ServicePlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public bool IsActive { get; set; }
    public List<PlanEntitlementDto> Entitlements { get; set; } = new();
}

public class PaymentOrderDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string VnpTxnRef { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentOrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentUrl { get; set; }
}

public class CreditLedgerDto
{
    public Guid Id { get; set; }
    public CreditType CreditType { get; set; }
    public int Quantity { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? PaymentOrderId { get; set; }
}

public class CreditBalanceDto
{
    public Dictionary<CreditType, int> Balances { get; set; } = new();
}
