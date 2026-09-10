using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("CandidateSkills")]
public sealed class CandidateSkill
{
    [Key]
    public Guid Id { get; set; }

    public Guid CandidateProfileId { get; set; }

    [ForeignKey(nameof(CandidateProfileId))]
    public CandidateProfile CandidateProfile { get; set; } = null!;

    [Required, MaxLength(100)]
    public string NormalizedName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}
