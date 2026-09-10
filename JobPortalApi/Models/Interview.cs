using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

public enum InterviewType
{
    Online,
    Onsite,
    Phone
}

public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled
}

public enum InterviewResult
{
    Pending,
    Passed,
    Failed
}

[Table("Interviews")]
public sealed class Interview
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public Job Application { get; set; } = null!;

    [Required]
    public InterviewType Type { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? MeetingUrl { get; set; }

    [Required]
    public Guid InterviewerId { get; set; }

    [ForeignKey(nameof(InterviewerId))]
    public User Interviewer { get; set; } = null!;

    [Required]
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    [Required]
    public InterviewResult Result { get; set; } = InterviewResult.Pending;

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
