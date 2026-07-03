using AspNetCoreRateLimit;
using QACopilot.API.Extensions;
using QACopilot.API.Hubs;
using QACopilot.API.Middleware;
using QACopilot.Infrastructure.DependencyInjection;
using Serilog;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QACopilot.Infrastructure.Services.ExternalServices;


var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecksConfig(builder.Configuration);
builder.Services.AddRateLimitingConfig(builder.Configuration);
builder.Services.AddHttpClient<JiraService>();
builder.Services.AddSignalR();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityAuditMiddleware>();
app.UseMiddleware<DomainValidationMiddleware>();
app.UseSerilogRequestLogging();
app.UseSecurityHeaders();
app.UseIpRateLimiting();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "QA Copilot API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<AppNotificationHub>("/hubs/notifications");
app.MapHub<ProjectHub>("/hubs/projects");
app.MapHub<ActivityHub>("/hubs/activity");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds + "ms"
            })
        }, new JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();

Log.Information("QA Copilot API started on {Environment}", app.Environment.EnvironmentName);

app.Run();