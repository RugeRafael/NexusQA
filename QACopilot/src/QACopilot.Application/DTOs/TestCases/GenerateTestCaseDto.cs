namespace QACopilot.Application.DTOs.TestCases;

public class GenerateTestCaseDto
{
    public Guid DocumentId { get; set; }
    public string? AdditionalContext { get; set; }
}