using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Jira;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Infrastructure.Services.ExternalServices;
using System.Text.RegularExpressions;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/jira")]
[Authorize]
public class JiraIntegrationController : ControllerBase
{
    private readonly IJiraService _jiraService;
    private readonly ILogger<JiraIntegrationController> _logger;

    public JiraIntegrationController(IJiraService jiraService,
        ILogger<JiraIntegrationController> logger)
    {
        _jiraService = jiraService;
        _logger = logger;
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var jiraServiceConcrete = _jiraService as JiraService;
        if (jiraServiceConcrete == null)
            return BadRequest(ApiResponse<object>.Fail("Servicio Jira no disponible"));

        var projects = await jiraServiceConcrete.GetProjectsAsync();
        return Ok(ApiResponse<object>.Ok(projects));
    }

    [HttpGet("bugs")]
    public async Task<IActionResult> GetBugsByProject([FromQuery] string projectKey, [FromQuery] string? assignee = null)
    {
        var jiraServiceConcrete = _jiraService as JiraService;
        if (jiraServiceConcrete == null)
            return BadRequest(ApiResponse<object>.Fail("Servicio Jira no disponible"));

        var bugs = await jiraServiceConcrete.GetBugsByProjectAsync(projectKey, assignee);
        return Ok(ApiResponse<object>.Ok(bugs));
    }

    [HttpPost("upload-to-project")]
    public async Task<IActionResult> UploadToProject([FromBody] UploadToProjectRequest request)
    {
        var jiraServiceConcrete = _jiraService as JiraService;
        if (jiraServiceConcrete == null)
            return BadRequest(ApiResponse<object>.Fail("Servicio Jira no disponible"));

        var projectKey = ExtractProjectKey(request.JiraUrl);
        if (string.IsNullOrEmpty(projectKey))
            return BadRequest(ApiResponse<object>.Fail(
                $"No se pudo extraer el proyecto de la URL: {request.JiraUrl}. " +
                "Formatos validos: /browse/PFD-436 o /projects/PFD/"));

        _logger.LogInformation("Uploading to Jira project: {Key} from URL: {Url}", projectKey, request.JiraUrl);

        var result = await _jiraService.CreateIssueAsync(new JiraIssueDto
        {
            ProjectKey = projectKey,
            Summary = request.Summary,
            Description = request.Description,
            IssueType = "Task",
            Priority = "Medium"
        });

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error creando issue en Jira"));

        return Ok(ApiResponse<object>.Ok(new
        {
            key = result.IssueKey,
            url = result.IssueUrl,
            projectKey,
            message = $"Informe subido al proyecto {projectKey} — {result.IssueKey}"
        }));
    }

    [HttpGet("bugs-by-url")]
    public async Task<IActionResult> GetBugsByUrl([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
            return BadRequest(ApiResponse<object>.Fail("URL requerida"));

        var jiraServiceConcrete = _jiraService as JiraService;
        if (jiraServiceConcrete == null)
            return BadRequest(ApiResponse<object>.Fail("Servicio Jira no disponible"));

        var issueKey = jiraServiceConcrete.ExtractIssueKeyFromUrl(url);
        if (string.IsNullOrEmpty(issueKey))
            return BadRequest(ApiResponse<object>.Fail(
                "No se pudo extraer el issue key de la URL. Formato esperado: /browse/XXX-000"));

        var bugs = await jiraServiceConcrete.GetBugsByIssueUrlAsync(url);
        return Ok(ApiResponse<object>.Ok(new { issueKey, bugs, total = bugs.Count }));
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        var ok = await _jiraService.TestConnectionAsync();
        return Ok(ApiResponse<object>.Ok(new { connected = ok },
            ok ? "Jira conectado correctamente" : "No se pudo conectar a Jira"));
    }

    [HttpGet("issues")]
    public async Task<IActionResult> GetIssues([FromQuery] int maxResults = 20)
    {
        var jiraServiceConcrete = _jiraService as JiraService;
        if (jiraServiceConcrete == null)
            return BadRequest(ApiResponse<object>.Fail("Servicio Jira no disponible"));

        var issues = await jiraServiceConcrete.GetProjectIssuesAsync(maxResults);
        return Ok(ApiResponse<object>.Ok(issues));
    }

    [HttpPost("testcase")]
    public async Task<IActionResult> CreateTestCase([FromBody] CreateJiraTestCaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
            return BadRequest(ApiResponse<object>.Fail("El resumen es requerido"));

        var issue = new JiraIssueDto
        {
            Summary = request.Summary,
            Description = request.Description,
            IssueType = "Task",
            Priority = request.Priority
        };

        var result = await _jiraService.CreateIssueAsync(issue);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error al crear issue"));

        return Ok(ApiResponse<object>.Ok(new
        {
            key = result.IssueKey,
            url = result.IssueUrl,
            message = $"Issue {result.IssueKey} creado en Jira"
        }));
    }

    [HttpPost("bug")]
    public async Task<IActionResult> CreateBug([FromBody] CreateJiraBugRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
            return BadRequest(ApiResponse<object>.Fail("El resumen es requerido"));

        var description = $"{request.Description}\n\nPasos para reproducir:\n{request.StepsToReproduce}";

        var issue = new JiraIssueDto
        {
            Summary = $"Bug: {request.Summary}",
            Description = description,
            IssueType = "Bug",
            Priority = request.Priority
        };

        var result = await _jiraService.CreateIssueAsync(issue);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error al crear bug"));

        return Ok(ApiResponse<object>.Ok(new
        {
            key = result.IssueKey,
            url = result.IssueUrl,
            message = $"Bug {result.IssueKey} creado en Jira"
        }));
    }

    // ─── HELPERS ───────────────────────────────────────────────────────────────

    private static string ExtractProjectKey(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";

        // /browse/PFD-436 o /browse/V20-888
        var browseMatch = Regex.Match(url, @"/browse/([A-Z][A-Z0-9]+)-\d+", RegexOptions.IgnoreCase);
        if (browseMatch.Success) return browseMatch.Groups[1].Value.ToUpper();

        // /projects/PFD/ o /projects/PFD/boards
        var projectMatch = Regex.Match(url, @"/projects/([A-Z][A-Z0-9]+)", RegexOptions.IgnoreCase);
        if (projectMatch.Success) return projectMatch.Groups[1].Value.ToUpper();

        // ?project=PFD o ?projectKey=PFD
        var queryMatch = Regex.Match(url, @"[?&]project(?:Key)?=([A-Z][A-Z0-9]+)", RegexOptions.IgnoreCase);
        if (queryMatch.Success) return queryMatch.Groups[1].Value.ToUpper();

        return "";
    }
}

// ─── REQUEST MODELS ────────────────────────────────────────────────────────────

public class UploadToProjectRequest
{
    public string JiraUrl { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateJiraTestCaseRequest
{
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}

public class CreateJiraBugRequest
{
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StepsToReproduce { get; set; } = string.Empty;
    public string Priority { get; set; } = "High";
}
