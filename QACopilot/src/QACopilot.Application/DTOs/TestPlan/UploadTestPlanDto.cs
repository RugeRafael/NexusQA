namespace QACopilot.Application.DTOs.TestPlan;

public class UploadTestPlanDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = [];
    public Guid? ProjectId { get; set; }
}