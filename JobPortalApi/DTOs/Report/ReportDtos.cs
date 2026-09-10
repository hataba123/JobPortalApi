using System.ComponentModel.DataAnnotations;

namespace JobPortalApi.DTOs.Report;

public sealed class CreateJobReportRequest
{
    [Required]
    public Guid JobPostId { get; init; }

    [Required, MaxLength(200)]
    public string Reason { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }
}

public sealed class UpdateJobReportRequest
{
    [Required]
    public string Status { get; init; } = string.Empty;
}

public sealed class JobReportDto
{
    public Guid Id { get; init; }
    public Guid JobPostId { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public Guid ReporterId { get; init; }
    public string ReporterEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string Version { get; init; } = string.Empty;
}
