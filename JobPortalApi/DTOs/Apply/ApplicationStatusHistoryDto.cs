using JobPortalApi.Models.Enums;

namespace JobPortalApi.DTOs.Apply;

public sealed class ApplicationStatusHistoryDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public ApplyStatus? FromStatus { get; init; }
    public ApplyStatus ToStatus { get; init; }
    public Guid? ChangedBy { get; init; }
    public DateTime ChangedAt { get; init; }
    public string? Reason { get; init; }
}
