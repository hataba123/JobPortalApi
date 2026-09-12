using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Middleware
{
    public sealed class ApiExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

        public ApiExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ApiExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await WriteErrorAsync(context, exception);
            }
        }

        private async Task WriteErrorAsync(HttpContext context, Exception exception)
        {
            var statusCode = exception switch
            {
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                ArgumentException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ApiConflictException => StatusCodes.Status409Conflict,
                DbUpdateException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
            if (statusCode >= 500)
                _logger.LogError(exception, "Unhandled API exception {TraceId}", context.TraceIdentifier);
            else
                // Không ghi nguyên exception của request vào log; một số lỗi xác thực
                // hoặc thanh toán có thể chứa token, email hay dữ liệu đối tác.
                _logger.LogWarning("Handled API exception {TraceId} {ExceptionType}",
                    context.TraceIdentifier, exception.GetType().Name);

            if (context.Response.HasStarted) return;
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var response = new
            {
                statusCode,
                code = exception is ApiConflictException conflict ? conflict.Code : ToCode(statusCode),
                message = statusCode >= 500 ? "Đã xảy ra lỗi máy chủ." : exception.Message,
                traceId = context.TraceIdentifier,
                details = exception is ApiConflictException conflictDetails ? conflictDetails.Details : null
            };
            await context.Response.WriteAsJsonAsync(response);
        }

        private static string ToCode(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "BAD_REQUEST",
            StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
            StatusCodes.Status403Forbidden => "FORBIDDEN",
            StatusCodes.Status404NotFound => "NOT_FOUND",
            StatusCodes.Status409Conflict => "CONFLICT",
            StatusCodes.Status429TooManyRequests => "RATE_LIMITED",
            StatusCodes.Status428PreconditionRequired => "PRECONDITION_REQUIRED",
            _ => "INTERNAL_SERVER_ERROR"
        };
    }
}
