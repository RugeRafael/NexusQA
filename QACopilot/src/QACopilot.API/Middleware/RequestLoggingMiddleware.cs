using System.Diagnostics;

namespace QACopilot.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8].ToUpper();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
        });

        _logger.LogInformation(
            "REQ [{RequestId}] {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        await _next(context);

        stopwatch.Stop();

        var level = context.Response.StatusCode >= 500
            ? Microsoft.Extensions.Logging.LogLevel.Error
            : context.Response.StatusCode >= 400
                ? Microsoft.Extensions.Logging.LogLevel.Warning
                : Microsoft.Extensions.Logging.LogLevel.Information;

        _logger.Log(level,
            "RES [{RequestId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}