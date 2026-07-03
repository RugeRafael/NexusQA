namespace QACopilot.Application.DTOs.Reports;

public class GenerateReportDto
{
    public Guid TemplateId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string? AdditionalContext { get; set; }
}