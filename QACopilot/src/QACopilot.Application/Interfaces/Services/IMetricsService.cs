using QACopilot.Application.DTOs.Metrics;

namespace QACopilot.Application.Interfaces.Services;

public interface IMetricsService
{
    Task<DashboardDto> GetDashboardAsync();
}