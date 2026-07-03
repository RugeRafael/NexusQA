namespace QACopilot.Application.DTOs.Reports;

public class ReportTemplateResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StructureJson { get; set; } = string.Empty;
    public string AIInstructions { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}