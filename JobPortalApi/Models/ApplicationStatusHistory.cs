using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.Models;

[Table("ApplicationStatusHistories")]
public sealed class ApplicationStatusHistory
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public Job Application { get; set; } = null!;

    public ApplyStatus? FromStatus { get; set; }

    [Required]
    public ApplyStatus ToStatus { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }
}
