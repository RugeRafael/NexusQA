using QACopilot.Application.DTOs.Jira;

namespace QACopilot.Application.Interfaces.Services;

public interface IJiraService
{
    Task<JiraResponseDto> CreateIssueAsync(JiraIssueDto issue);
    Task<bool> TestConnectionAsync();
}