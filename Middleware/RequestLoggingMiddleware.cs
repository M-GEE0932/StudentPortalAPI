using System.Diagnostics;

namespace StudentPortalAPI.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("[{RequestId}] {Method} {Path}",
            requestId, context.Request.Method, context.Request.Path);

        context.Items["RequestId"] = requestId;

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation("[{RequestId}] {StatusCode} completed in {Elapsed}ms",
            requestId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
}
