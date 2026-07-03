using QACopilot.Infrastructure.Data.Context;
using QACopilot.Domain.Entities;

namespace QACopilot.API.Middleware;

public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    private static readonly string[] SensitivePaths =
        ["/api/auth", "/api/admin", "/api/training", "/api/metrics"];

    private static readonly Dictionary<string, string> ModuleMap = new()
    {
        { "/api/auth", "Auth" },
        { "/api/documents", "Documents" },
        { "/api/testcases", "TestCases" },
        { "/api/history", "History" },
        { "/api/metrics", "Metrics" },
        { "/api/projects", "Projects" },
        { "/api/chat", "Chat" },
        { "/api/training", "Training" },
        { "/api/reports", "Reports" }
    };

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, QACopilotDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        var isSensitive = SensitivePaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (isSensitive)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.User.FindFirst("uid")?.Value ?? "anonymous";
            _logger.LogInformation(
                "SECURITY_AUDIT | Path: {Path} | Method: {Method} | UserId: {UserId} | IP: {IP}",
                path, method, userId, ip);
        }

        await _next(context);

        var statusCode = context.Response.StatusCode;

        if (statusCode is 401 or 403)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogWarning(
                "SECURITY_BLOCKED | Status: {Status} | Path: {Path} | IP: {IP}",
                statusCode, path, ip);
        }

        // Registrar en AuditMetrics solo para POST/PUT/DELETE exitosos o fallidos
        if (method is "POST" or "PUT" or "DELETE" or "GET")
        {
            var module = ModuleMap.FirstOrDefault(m =>
                path.StartsWith(m.Key, StringComparison.OrdinalIgnoreCase)).Value;

            if (module != null && !path.Contains("/hubs/") && !path.Contains("/negotiate"))
            {
                try
                {
                    var userIdStr = context.User.FindFirst("uid")?.Value;
                    Guid? userId = userIdStr != null ? Guid.Parse(userIdStr) : null;

                   var audit = new AuditMetric
{
    Id = Guid.NewGuid(),
    UserId = userId ?? Guid.Empty,
    Module = module,
    Action = $"{method} {path}",
    Success = statusCode >= 200 && statusCode < 400,
    OccurredAt = DateTime.UtcNow,
    IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
};

                    db.AuditMetrics.Add(audit);
                    await db.SaveChangesAsync();
                }
                catch { /* No romper la request si falla el audit */ }
            }
        }
    }
}