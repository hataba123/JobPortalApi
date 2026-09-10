namespace JobPortalApi.DTOs.Shared;

using System.Text.Json.Serialization;

public sealed class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    [JsonIgnore]
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
