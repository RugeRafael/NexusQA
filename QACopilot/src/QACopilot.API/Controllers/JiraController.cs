using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Jira;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/jira")]
[Authorize]
public class JiraController : ControllerBase
{
    private readonly IJiraService _jiraService;

    public JiraController(IJiraService jiraService)
    {
        _jiraService = jiraService;
    }

    [HttpPost("issues")]
    public async Task<IActionResult> CreateIssue([FromBody] JiraIssueDto request)
    {
        var result = await _jiraService.CreateIssueAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse<JiraResponseDto>.Fail(
                result.ErrorMessage ?? "Failed to create Jira issue."));

        return Ok(ApiResponse<JiraResponseDto>.Ok(result, "Jira issue created successfully."));
    }

    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        var isConnected = await _jiraService.TestConnectionAsync();
        return Ok(ApiResponse<object>.Ok(
            new { connected = isConnected },
            isConnected ? "Jira connection successful." : "Jira connection failed."));
    }
}