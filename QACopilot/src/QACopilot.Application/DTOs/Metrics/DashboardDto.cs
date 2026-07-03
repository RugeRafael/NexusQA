namespace QACopilot.Application.DTOs.Metrics;

public class DashboardDto
{
    public int TotalDocuments { get; set; }
    public int TotalTestCasesGenerated { get; set; }
    public double AverageConfidenceScore { get; set; }
    public int TotalUsers { get; set; }
    public IEnumerable<ModuleActivityDto> ActivityByModule { get; set; } = [];
}