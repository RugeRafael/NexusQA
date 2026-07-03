namespace QACopilot.Application.DTOs.Analytics;

public class UserActivityDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int TotalTestCasesGenerated { get; set; }
    public int TotalTestPlansAnalyzed { get; set; }
    public long TotalSessionSeconds { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public IEnumerable<string> AssignedProjects { get; set; } = [];
}