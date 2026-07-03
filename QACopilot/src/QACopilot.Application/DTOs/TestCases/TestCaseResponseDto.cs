namespace QACopilot.Application.DTOs.TestCases;

public class TestCaseResponseDto
{
    public Guid Id { get; set; }
    public string GeneratedContent { get; set; } = string.Empty;
    public int TotalTestCases { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; }
}