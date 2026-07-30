using System.Net;
using System.Text.Json;
using QACopilot.API.Helpers;
using QACopilot.Domain.Exceptions;

namespace QACopilot.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errorCode) = ex switch
        {
            AiServiceException aiEx => (aiEx.StatusCode, aiEx.Message, aiEx.ErrorCode),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message, "NOT_FOUND"),
            UnauthorizedException => (HttpStatusCode.Forbidden, ex.Message, "FORBIDDEN"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message, "UNAUTHORIZED"),
            ValidationException => (HttpStatusCode.BadRequest, ex.Message, "VALIDATION_ERROR"),
            InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message, "BAD_REQUEST"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", "UNKNOWN")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errorCode: errorCode);
        var json = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}
