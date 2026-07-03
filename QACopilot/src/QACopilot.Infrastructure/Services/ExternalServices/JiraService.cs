using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Jira;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.Infrastructure.Services.ExternalServices;

public class JiraService : IJiraService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JiraService> _logger;
    private readonly string _baseUrl;
    private readonly string _projectKey;

    public JiraService(HttpClient httpClient, IConfiguration config, ILogger<JiraService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var email = config["Jira:Email"] ?? "";
        var token = config["Jira:ApiToken"] ?? "";
        _baseUrl = config["Jira:BaseUrl"] ?? "https://soporteithealth.atlassian.net";
        _projectKey = config["Jira:ProjectKey"] ?? "SEQ";

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{token}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<JiraResponseDto> CreateIssueAsync(JiraIssueDto issue)
    {
        try
        {
            var payload = new
            {
                fields = new
                {
                    project = new { key = _projectKey },
                    summary = $"[QA Copilot] {issue.Summary}",
                    description = new
                    {
                        type = "doc",
                        version = 1,
                        content = new[]
                        {
                            new
                            {
                                type = "paragraph",
                                content = new[]
                                {
                                    new { type = "text", text = issue.Description ?? "" }
                                }
                            }
                        }
                    },
                    issuetype = new { name = issue.IssueType ?? "Task" },
                    priority = new { name = issue.Priority ?? "Medium" },
                    labels = new[] { "qa-copilot", issue.IssueType?.ToLower() ?? "task" }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/rest/api/3/issue", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Jira create issue failed: {Status} — {Body}",
                    response.StatusCode, responseBody);
                return new JiraResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Error {(int)response.StatusCode}: {responseBody}"
                };
            }

            var result = JsonSerializer.Deserialize<JiraCreateResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new JiraResponseDto
            {
                Success = true,
                IssueKey = result?.Key ?? "",
                IssueUrl = $"{_baseUrl}/browse/{result?.Key}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Jira issue");
            return new JiraResponseDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rest/api/3/myself");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    public async Task<List<JiraProject>> GetProjectsAsync()
{
    try
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/rest/api/3/project");
        if (!response.IsSuccessStatusCode) return new List<JiraProject>();

        var json = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<List<JiraProjectRaw>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return projects?.Select(p => new JiraProject
        {
            Key = p.Key ?? "",
            Name = p.Name ?? "",
            Id = p.Id ?? ""
        }).ToList() ?? new List<JiraProject>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting Jira projects");
        return new List<JiraProject>();
    }
}

public async Task<List<JiraIssue>> GetBugsByProjectAsync(string projectKey, string? assignee = null)
{
    try
    {
        var jql = $"project={projectKey} AND issuetype=Bug ORDER BY created DESC";
        if (!string.IsNullOrEmpty(assignee))
            jql = $"project={projectKey} AND issuetype=Bug AND assignee=\"{assignee}\" ORDER BY created DESC";

        var url = $"{_baseUrl}/rest/api/3/search?jql={Uri.EscapeDataString(jql)}&maxResults=50";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode) return new List<JiraIssue>();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JiraSearchResult>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.Issues?.Select(i => new JiraIssue
        {
            Key = i.Key,
            Summary = i.Fields?.Summary ?? "",
            Status = i.Fields?.Status?.Name ?? "",
            Priority = i.Fields?.Priority?.Name ?? "",
            IssueType = i.Fields?.Issuetype?.Name ?? "",
            Created = i.Fields?.Created ?? DateTime.UtcNow,
            Url = $"{_baseUrl}/browse/{i.Key}"
        }).ToList() ?? new List<JiraIssue>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting bugs by project");
        return new List<JiraIssue>();
    }
}

public string ExtractProjectKeyFromUrl(string url)
{
    try
    {
        // Patrones: /projects/SEQ/ o /project/SEQ o ?project=SEQ
        var patterns = new[]
        {
            @"/projects/([A-Z]+)/",
            @"/projects/([A-Z]+)$",
            @"/project/([A-Z]+)",
            @"project=([A-Z]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
            if (match.Success) return match.Groups[1].Value;
        }
        return string.Empty;
    }
    catch { return string.Empty; }
}

    public async Task<List<JiraIssue>> GetProjectIssuesAsync(int maxResults = 20)
    {
        try
        {
            var url = $"{_baseUrl}/rest/api/3/search?jql=project={_projectKey} AND labels=qa-copilot ORDER BY created DESC&maxResults={maxResults}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return new List<JiraIssue>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JiraSearchResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Issues?.Select(i => new JiraIssue
            {
                Key = i.Key,
                Summary = i.Fields?.Summary ?? "",
                Status = i.Fields?.Status?.Name ?? "",
                Priority = i.Fields?.Priority?.Name ?? "",
                IssueType = i.Fields?.Issuetype?.Name ?? "",
                Created = i.Fields?.Created ?? DateTime.UtcNow,
                Url = $"{_baseUrl}/browse/{i.Key}"
            }).ToList() ?? new List<JiraIssue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Jira issues");
            return new List<JiraIssue>();
        }
    }
public async Task<List<JiraIssue>> GetBugsByIssueUrlAsync(string issueUrl)
{
    try
    {
        var issueKey = ExtractIssueKeyFromUrl(issueUrl);
        if (string.IsNullOrEmpty(issueKey))
            return new List<JiraIssue>();

        var bugs = new List<JiraIssue>();

        var r = await _httpClient.GetAsync(
            $"{_baseUrl}/rest/api/3/issue/{issueKey}?fields=subtasks,summary,status,priority,issuetype");

        if (!r.IsSuccessStatusCode) return bugs;

        var rawJson = await r.Content.ReadAsStringAsync();
        _logger.LogInformation("Jira raw response for {Key}: {Json}", issueKey, rawJson[..Math.Min(rawJson.Length, 500)]);

        using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("fields", out var fields) ||
            !fields.TryGetProperty("subtasks", out var subtasks))
        {
            _logger.LogWarning("No subtasks found for {Key}", issueKey);
            return bugs;
        }

        _logger.LogInformation("Subtasks count: {Count}", subtasks.GetArrayLength());

        foreach (var subtask in subtasks.EnumerateArray())
        {
            var key = subtask.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(key)) continue;

            _logger.LogInformation("Subtask key found: {Key}", key);

            // Extraer datos directamente del subtask en la respuesta principal
            var sf = subtask.TryGetProperty("fields", out var f) ? f : default;

            var summary = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                          sf.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";

            var status = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                         sf.TryGetProperty("status", out var st) && st.ValueKind != System.Text.Json.JsonValueKind.Null &&
                         st.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "";

            var priority = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                           sf.TryGetProperty("priority", out var pr) && pr.ValueKind != System.Text.Json.JsonValueKind.Null &&
                           pr.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "";

            var issueType = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                            sf.TryGetProperty("issuetype", out var it) && it.ValueKind != System.Text.Json.JsonValueKind.Null &&
                            it.TryGetProperty("name", out var itn) ? itn.GetString() ?? "" : "";

            bugs.Add(new JiraIssue
            {
                Key = key,
                Summary = summary,
                Status = status,
                Priority = priority,
                IssueType = issueType,
                Description = "",
                Assignee = "Ver en Jira",
                Created = DateTime.UtcNow,
                Url = $"{_baseUrl}/browse/{key}"
            });
        }

        _logger.LogInformation("Total bugs mapped: {Count}", bugs.Count);
        return bugs;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting bugs by issue URL");
        return new List<JiraIssue>();
    }
}

private async Task<JiraIssue?> GetIssueDetailAsync(string issueKey)
{
    try
    {
        var r = await _httpClient.GetAsync(
            $"{_baseUrl}/rest/api/3/issue/{issueKey}?fields=summary,status,priority,issuetype,description,assignee,created");
        
        _logger.LogInformation("GetIssueDetail {Key}: Status={Status}", issueKey, r.StatusCode);
        
        if (!r.IsSuccessStatusCode) return null;

        var json = await r.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("fields", out var f))
        {
            _logger.LogWarning("No fields for {Key}", issueKey);
            return null;
        }

        var issue = new JiraIssue
        {
            Key = issueKey,
            Summary = f.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
            Status = f.TryGetProperty("status", out var st) && st.ValueKind != System.Text.Json.JsonValueKind.Null &&
                     st.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "",
            Priority = f.TryGetProperty("priority", out var pr) && pr.ValueKind != System.Text.Json.JsonValueKind.Null &&
                       pr.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "",
            IssueType = f.TryGetProperty("issuetype", out var it) && it.ValueKind != System.Text.Json.JsonValueKind.Null &&
                        it.TryGetProperty("name", out var itn) ? itn.GetString() ?? "" : "",
            Description = ExtractDescription(f),
            Assignee = f.TryGetProperty("assignee", out var asgn) && asgn.ValueKind != System.Text.Json.JsonValueKind.Null &&
                       asgn.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "Sin asignar",
            Created = f.TryGetProperty("created", out var cr) && cr.ValueKind != System.Text.Json.JsonValueKind.Null ?
                      cr.GetDateTime() : DateTime.UtcNow,
            Url = $"{_baseUrl}/browse/{issueKey}"
        };

        _logger.LogInformation("Mapped issue {Key}: Summary={Summary}", issueKey, issue.Summary);
        return issue;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting detail for {Key}", issueKey);
        return null;
    }
}

private string ExtractDescription(System.Text.Json.JsonElement fields)
{
    try
    {
        if (!fields.TryGetProperty("description", out var desc) ||
            desc.ValueKind == System.Text.Json.JsonValueKind.Null)
            return "Sin descripción";

        // Jira usa Atlassian Document Format (ADF)
        if (desc.TryGetProperty("content", out var content))
        {
            var texts = new List<string>();
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("content", out var inner))
                {
                    foreach (var node in inner.EnumerateArray())
                    {
                        if (node.TryGetProperty("text", out var text))
                            texts.Add(text.GetString() ?? "");
                    }
                }
            }
            return string.Join(" ", texts).Trim();
        }

        return desc.GetString() ?? "Sin descripción";
    }
    catch { return "Sin descripción"; }
}

private JiraIssue MapIssueDetailed(JiraIssueItem i) => new JiraIssue
{
    Key = i.Key ?? "",
    Summary = i.Fields?.Summary ?? "",
    Status = i.Fields?.Status?.Name ?? "",
    Priority = i.Fields?.Priority?.Name ?? "",
    IssueType = i.Fields?.Issuetype?.Name ?? "",
    Description = i.Fields?.Description ?? "Sin descripción",
    Assignee = i.Fields?.Assignee?.DisplayName ?? "Sin asignar",
    Created = i.Fields?.Created ?? DateTime.UtcNow,
    Url = $"{_baseUrl}/browse/{i.Key}"
};

private JiraIssue MapIssue(JiraIssueItem i) => new JiraIssue
{
    Key = i.Key ?? "",
    Summary = i.Fields?.Summary ?? "",
    Status = i.Fields?.Status?.Name ?? "",
    Priority = i.Fields?.Priority?.Name ?? "",
    IssueType = i.Fields?.Issuetype?.Name ?? "",
    Created = i.Fields?.Created ?? DateTime.UtcNow,
    Url = $"{_baseUrl}/browse/{i.Key}"
};

public string ExtractIssueKeyFromUrl(string url)
{
    try
    {
        // Patrón: /browse/V20-888 o /issues/V20-888
        var match = System.Text.RegularExpressions.Regex.Match(
            url, @"/browse/([A-Z0-9]+-[0-9]+)|/issues/([A-Z0-9]+-[0-9]+)");
        if (match.Success)
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return string.Empty;
    }
    catch { return string.Empty; }
}

}

public class JiraIssue
{
    public string Key { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime Created { get; set; }
}

public class JiraSearchResult
{
    public List<JiraIssueItem>? Issues { get; set; }
}

public class JiraIssueItem
{
    public string Key { get; set; } = string.Empty;
    public JiraIssueFields? Fields { get; set; }
}

public class JiraIssueFields
{
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public JiraStatus? Status { get; set; }
    public JiraPriority? Priority { get; set; }
    public JiraIssueType? Issuetype { get; set; }
    public JiraProject? Project { get; set; }
    public JiraAssignee? Assignee { get; set; }
    public DateTime Created { get; set; }
}
public class JiraAssignee
{
    public string? DisplayName { get; set; }
    public string? EmailAddress { get; set; }
}

public class JiraStatus { public string Name { get; set; } = string.Empty; }
public class JiraPriority { public string Name { get; set; } = string.Empty; }
public class JiraIssueType { public string Name { get; set; } = string.Empty; }
public class JiraCreateResponse
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
public class JiraProject
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class JiraProjectRaw
{
    public string? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
}
public class JiraIssueRaw
{
    public string Key { get; set; } = string.Empty;
    public JiraIssueFields? Fields { get; set; }
}



