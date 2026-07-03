using System.Security.Claims;
using QACopilot.API.Helpers;
using System.Text.Json;

namespace QACopilot.API.Middleware;

public class DomainValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainValidationMiddleware> _logger;
    private const string AllowedDomain = "@ithealth.co";

    private static readonly string[] ExcludedPaths =
        ["/api/auth/login", "/api/auth/register", "/health", "/swagger", "/openapi"];

    public DomainValidationMiddleware(
        RequestDelegate next,
        ILogger<DomainValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isExcluded = ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isExcluded && context.User.Identity?.IsAuthenticated == true)
        {
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.FindFirst("email")?.Value ?? string.Empty;

            if (!email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Access denied for unauthorized domain. Email: {Email}, Path: {Path}",
                    email, path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.Fail(
                    "Access restricted to ithealth.co domain only.");
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                return;
            }
        }

        await _next(context);
    }
}