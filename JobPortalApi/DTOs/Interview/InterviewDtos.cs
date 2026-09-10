using System.ComponentModel.DataAnnotations;
using JobPortalApi.Models;

namespace JobPortalApi.DTOs.Interview;

public sealed class CreateInterviewRequest
{
    [Required]
    public InterviewType Type { get; init; }

    [Required]
    public DateTime StartAt { get; init; }

    [Required]
    public DateTime EndAt { get; init; }

    [MaxLength(500)]
    public string? Location { get; init; }

    [MaxLength(500)]
    public string? MeetingUrl { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }
}

public sealed class UpdateInterviewRequest
{
    public InterviewType? Type { get; init; }
    public DateTime? StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    [MaxLength(500)] public string? Location { get; init; }
    [MaxLength(500)] public string? MeetingUrl { get; init; }
    [MaxLength(4000)] public string? Notes { get; init; }
}

public sealed class CompleteInterviewRequest
{
    [Required]
    public InterviewResult Result { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }

    public string? ApplicationVersion { get; init; }
}

public sealed class InterviewDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public Guid JobPostId { get; init; }
    public Guid CandidateId { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public InterviewType Type { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string? Location { get; init; }
    public string? MeetingUrl { get; init; }
    public Guid InterviewerId { get; init; }
    public InterviewStatus Status { get; init; }
    public InterviewResult Result { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string Version { get; init; } = string.Empty;
    public string ApplicationVersion { get; init; } = string.Empty;
}
