namespace QACopilot.Application.DTOs.Jira;

public class JiraResponseDto
{
    public string IssueKey { get; set; } = string.Empty;
    public string IssueUrl { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}