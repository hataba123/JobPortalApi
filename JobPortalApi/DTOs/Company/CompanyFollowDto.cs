namespace JobPortalApi.DTOs.Company;

public sealed class CompanyFollowDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public DateTime FollowedAt { get; set; }
}
