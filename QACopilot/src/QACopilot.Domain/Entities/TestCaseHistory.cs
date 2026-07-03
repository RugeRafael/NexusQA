namespace QACopilot.Domain.Entities;

public class TestCaseHistory
{
    public Guid Id { get; set; }
    public string GeneratedContent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalTestCases { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}