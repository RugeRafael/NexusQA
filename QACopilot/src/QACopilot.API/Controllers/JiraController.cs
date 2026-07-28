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

    [HttpPost("issues/{issueKey}/attach")]
    public async Task<IActionResult> AttachFile(string issueKey, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var success = await _jiraService.AttachFileAsync(
            issueKey, file.FileName, ms.ToArray(), file.ContentType);

        if (!success)
            return BadRequest(ApiResponse<object>.Fail("Error al adjuntar el archivo en Jira."));

        return Ok(ApiResponse<object>.Ok(new { attached = true, issueKey }, "Archivo adjuntado."));
    }

    [HttpPost("issues/create-with-attachment")]
    public async Task<IActionResult> CreateIssueWithAttachment(
        [FromForm] string summary,
        [FromForm] string description,
        [FromForm] string? issueType,
        [FromForm] string? priority,
        [FromForm] string? jiraUrl,
        IFormFile? file)
    {
        string? projectKey = null;
        string? parentIssueKey = null;

        if (!string.IsNullOrEmpty(jiraUrl))
        {
            // Si la URL es una issue específica (ej: /browse/LL26-4219) → crear subtarea
            var issueMatch = System.Text.RegularExpressions.Regex.Match(
                jiraUrl, @"/browse/([A-Z][A-Z0-9]+-\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (issueMatch.Success)
            {
                parentIssueKey = issueMatch.Groups[1].Value.ToUpper();
                projectKey = parentIssueKey.Split('-')[0];
            }
            else
            {
                // URL de proyecto → extraer project key
                projectKey = ExtractProjectKeyFromUrl(jiraUrl);
            }
        }

        var issueDto = new JiraIssueDto
        {
            Summary = summary,
            Description = description,
            IssueType = issueType ?? "Task",
            Priority = priority ?? "Medium",
            ProjectKey = projectKey ?? string.Empty,
            ParentIssueKey = parentIssueKey
        };

        var result = await _jiraService.CreateIssueAsync(issueDto);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error al crear la tarea."));

        bool attached = false;
        if (file != null && file.Length > 0)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            attached = await _jiraService.AttachFileAsync(
                result.IssueKey, file.FileName, ms.ToArray(), file.ContentType);
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            key = result.IssueKey,
            url = result.IssueUrl,
            projectKey = projectKey ?? "SEQ",
            parentIssueKey,
            attached
        }, "Tarea creada exitosamente."));
    }

    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        var isConnected = await _jiraService.TestConnectionAsync();
        return Ok(ApiResponse<object>.Ok(
            new { connected = isConnected },
            isConnected ? "Jira connection successful." : "Jira connection failed."));
    }

    private static string? ExtractProjectKeyFromUrl(string url)
    {
        var patterns = new[]
        {
            @"/projects/([A-Z][A-Z0-9]+)",
            @"/project/([A-Z][A-Z0-9]+)",
            @"[?&]project=([A-Z][A-Z0-9]+)",
            @"/browse/([A-Z][A-Z0-9]+)-\d+"
        };
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value.ToUpper();
        }
        return null;
    }
[HttpGet("issue-types/{projectKey}")]
[AllowAnonymous]
public async Task<IActionResult> GetIssueTypes(string projectKey)
{
    var url = $"https://soporteithealth.atlassian.net/rest/api/3/project/{projectKey}/issuetypes";
    var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("jira");
    var response = await httpClient.GetAsync(url);
    var body = await response.Content.ReadAsStringAsync();
    return Ok(body);
}
}
