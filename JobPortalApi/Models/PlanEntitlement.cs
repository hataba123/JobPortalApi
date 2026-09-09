using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.Models;

[Table("PlanEntitlements")]
public class PlanEntitlement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PlanId { get; set; }

    public CreditType CreditType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(1, int.MaxValue)]
    public int? ExpiresInDays { get; set; }

    [ForeignKey(nameof(PlanId))]
    public ServicePlan Plan { get; set; } = null!;
}
