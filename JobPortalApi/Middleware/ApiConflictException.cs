namespace JobPortalApi.Middleware;

/// <summary>
/// Lỗi nghiệp vụ dùng khi request xung đột với dữ liệu hiện có.
/// </summary>
public sealed class ApiConflictException : Exception
{
    public string Code { get; }
    public object? Details { get; }

    public ApiConflictException(string message, string code = "CONFLICT", object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}
