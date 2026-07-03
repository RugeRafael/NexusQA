namespace QACopilot.Application.DTOs.TestCases;

public class AIGenerationResultDto
{
    public string Content { get; set; } = string.Empty;
    public int TotalTestCases { get; set; }
    public double ConfidenceScore { get; set; }
}