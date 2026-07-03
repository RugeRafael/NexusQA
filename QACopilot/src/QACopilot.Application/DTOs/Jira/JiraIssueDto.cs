namespace QACopilot.Application.DTOs.Jira;

public class JiraIssueDto
{
    public string ProjectKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IssueType { get; set; } = "Task";
    public string Priority { get; set; } = "Medium";
}