using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Jobs")]
public class Job
{
    // JobPostId và CandidateId đi kèm navigation property có cùng tên (JobPost, Candidate) → EF tự suy ra foreign key.

    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid JobPostId { get; set; }
    [ForeignKey(nameof(JobPostId))]
    public JobPost JobPost { get; set; }

    [Required]
    public Guid CandidateId { get; set; }
    [ForeignKey(nameof(CandidateId))]
    public User Candidate { get; set; }

    [Required]
    public DateTime AppliedAt { get; set; }

    [MaxLength(300)]
    public string CVUrl { get; set; }
    public ApplyStatus Status { get; set; } = ApplyStatus.Pending;
}