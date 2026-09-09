namespace JobPortalApi.Middleware;

/// <summary>
/// Lỗi nghiệp vụ dùng khi request xung đột với dữ liệu hiện có.
/// </summary>
public sealed class ApiConflictException : Exception
{
    public ApiConflictException(string message)
        : base(message)
    {
    }
}
