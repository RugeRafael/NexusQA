using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<QACopilotDbContext>(
                name: "sqlserver",
                tags: ["database", "infrastructure"]);

        return services;
    }
}