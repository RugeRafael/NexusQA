namespace QACopilot.Application.DTOs.Reports;

public class ReportResponseDto
{
    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string QAEngineer { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}