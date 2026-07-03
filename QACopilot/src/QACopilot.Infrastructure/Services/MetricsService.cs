using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Metrics;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Services;

public class MetricsService : IMetricsService
{
    private readonly QACopilotDbContext _context;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(QACopilotDbContext context, ILogger<MetricsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        _logger.LogInformation("Fetching dashboard metrics");

        var totalDocuments = await _context.Documents.CountAsync();
        var totalTestCases = await _context.TestCaseHistories.SumAsync(t => t.TotalTestCases);

        double avgConfidence = 0;
        if (await _context.TestCaseHistories.AnyAsync())
        {
            var scores = await _context.TestCaseHistories
                .Select(t => (double)t.ConfidenceScore)
                .ToListAsync();
            avgConfidence = scores.Average();
        }

        var totalUsers = await _context.Users.CountAsync(u => u.IsActive);

        var activityByModule = await _context.AuditMetrics
            .GroupBy(a => a.Module)
            .Select(g => new ModuleActivityDto
            {
                Module = g.Key,
                TotalActions = g.Count(),
                SuccessCount = g.Count(a => a.Success),
                FailureCount = g.Count(a => !a.Success)
            })
            .ToListAsync();

        return new DashboardDto
        {
            TotalDocuments = totalDocuments,
            TotalTestCasesGenerated = totalTestCases,
            AverageConfidenceScore = Math.Round(avgConfidence, 2),
            TotalUsers = totalUsers,
            ActivityByModule = activityByModule
        };
    }
}