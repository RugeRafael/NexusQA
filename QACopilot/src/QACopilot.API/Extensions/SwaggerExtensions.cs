namespace QACopilot.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddOpenApi();

        services.AddSwaggerGen();

        return services;
    }
}