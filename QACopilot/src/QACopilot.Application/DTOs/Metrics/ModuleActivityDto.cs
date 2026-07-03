namespace QACopilot.Application.DTOs.Metrics;

public class ModuleActivityDto
{
    public string Module { get; set; } = string.Empty;
    public int TotalActions { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}