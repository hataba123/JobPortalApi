using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("OAuthAccounts")]
public class OAuthAccount
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    [Required, MaxLength(40)]
    public string Provider { get; set; }

    [Required, MaxLength(255)]
    public string ProviderAccountId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
