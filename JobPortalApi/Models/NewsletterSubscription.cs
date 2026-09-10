using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortalApi.Models;

[Table("NewsletterSubscriptions")]
public sealed class NewsletterSubscription
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
}
