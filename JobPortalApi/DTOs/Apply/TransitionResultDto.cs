using JobPortalApi.Models.Enums;

namespace JobPortalApi.DTOs.Apply;

public sealed class TransitionResultDto
{
    public Guid Id { get; init; }
    public ApplyStatus Status { get; init; }
    public string Version { get; init; } = string.Empty;
}
