using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Metrics;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/metrics")]
[Authorize(Policy = "AdminOnly")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;

    public MetricsController(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _metricsService.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }
}