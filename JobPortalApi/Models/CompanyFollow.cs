using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("CompanyFollows")]
public sealed class CompanyFollow
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public Guid CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;

    [Required]
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}
