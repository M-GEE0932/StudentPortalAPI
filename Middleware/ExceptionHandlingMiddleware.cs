using System.Net;
using System.Text.Json;

namespace StudentPortalAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            var requestId = context.Items["RequestId"] as string ?? "Unknown";
            _logger.LogError(ex, "An unhandled exception occurred for RequestId: {RequestId}", requestId);
            await HandleExceptionAsync(context, ex, requestId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new { error = message, statusCode = (int)statusCode, requestId };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}