namespace JobPortalApi.DTOs.User;

public sealed class UserSettingsDto
{
    public string? Phone { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool JobAlerts { get; set; } = true;
    public bool MarketingEmails { get; set; }
    public bool ProfileVisibility { get; set; } = true;
    public bool ApplicationUpdates { get; set; } = true;
}
