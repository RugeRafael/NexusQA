namespace QACopilot.Application.DTOs.Reports;

public class CreateReportTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StructureJson { get; set; } = string.Empty;
    public string AIInstructions { get; set; } = string.Empty;
}