using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using QACopilot.Application.Interfaces;
using QACopilot.Application.Interfaces.Repositories;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Infrastructure.Data.Context;
using QACopilot.Infrastructure.Repositories;
using QACopilot.Infrastructure.Services;
using QACopilot.Infrastructure.Services.ExternalServices;

namespace QACopilot.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<QACopilotDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(
                    typeof(QACopilotDbContext).Assembly.FullName)));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ITestCaseHistoryRepository, TestCaseHistoryRepository>();
        services.AddScoped<IAuditMetricRepository, AuditMetricRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpClient<IReportService, ReportService>();
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ITestCaseService, TestCaseService>();
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityTrackingService, ActivityTrackingService>();
        services.AddHttpClient<IAIService, AIService>()
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        services.AddHttpClient<IJiraService, JiraService>()
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(2,
                    attempt => TimeSpan.FromSeconds(attempt)));

        return services;
    }
}