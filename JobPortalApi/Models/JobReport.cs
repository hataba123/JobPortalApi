using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("JobReports")]
public sealed class JobReport
{
    [Key]
    public Guid Id { get; set; }

    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
