using AspNetCoreRateLimit;

namespace QACopilot.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimitingConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }
}