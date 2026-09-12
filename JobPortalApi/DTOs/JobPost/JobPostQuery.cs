namespace JobPortalApi.DTOs.JobPost;

public sealed class JobPostQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Location { get; init; }
    public string? Type { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinSalary { get; init; }
}
