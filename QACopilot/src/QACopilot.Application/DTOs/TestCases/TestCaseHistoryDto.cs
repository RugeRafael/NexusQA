namespace QACopilot.Application.DTOs.TestCases;

public class TestCaseHistoryDto
{
    public Guid Id { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int TotalTestCases { get; set; }
    public double ConfidenceScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}