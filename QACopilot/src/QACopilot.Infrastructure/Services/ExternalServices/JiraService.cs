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
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<JiraResponseDto> CreateIssueAsync(JiraIssueDto issue)
    {
        try
        {
            var projectKey = !string.IsNullOrEmpty(issue.ProjectKey) ? issue.ProjectKey : _projectKey;
            var hasParent = !string.IsNullOrEmpty(issue.ParentIssueKey);

            object payload;
            if (hasParent)
            {
                payload = new
                {
                    fields = new
                    {
                        project = new { key = projectKey },
                        summary = $"[NexusQA] {issue.Summary}",
                        description = new
                        {
                            type = "doc", version = 1,
                            content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text = issue.Description ?? "" } } } }
                        },
                        issuetype = new { name = "Sub-task" },
                        priority = new { name = issue.Priority ?? "Medium" },
                        parent = new { key = issue.ParentIssueKey }
                    }
                };
            }
            else
            {
                payload = new
                {
                    fields = new
                    {
                        project = new { key = projectKey },
                        summary = $"[NexusQA] {issue.Summary}",
                        description = new
                        {
                            type = "doc", version = 1,
                            content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text = issue.Description ?? "" } } } }
                        },
                        issuetype = new { name = issue.IssueType ?? "Task" },
                        priority = new { name = issue.Priority ?? "Medium" },
                        labels = new[] { "nexusqa", issue.IssueType?.ToLower() ?? "task" }
                    }
                };
            }

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/rest/api/3/issue", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Jira create issue failed: {Status} - {Body}", response.StatusCode, responseBody);
                return new JiraResponseDto { Success = false, ErrorMessage = $"Error {(int)response.StatusCode}: {responseBody}" };
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
        catch { return false; }
    }

    public async Task<bool> AttachFileAsync(string issueKey, string fileName, byte[] fileContent, string contentType)
    {
        try
        {
            var url = $"{_baseUrl}/rest/api/3/issue/{issueKey}/attachments";
            using var form = new MultipartFormDataContent();
            var fileBytes = new ByteArrayContent(fileContent);
            fileBytes.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileBytes, "file", fileName);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Atlassian-Token", "no-check");
            request.Content = form;
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Jira attach failed: {Status} - {Body}", response.StatusCode, body);
                return false;
            }
            _logger.LogInformation("File {FileName} attached to {IssueKey}", fileName, issueKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching file to Jira issue {IssueKey}", issueKey);
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
            return projects?.Select(p => new JiraProject { Key = p.Key ?? "", Name = p.Name ?? "", Id = p.Id ?? "" }).ToList() ?? new List<JiraProject>();
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
            var result = JsonSerializer.Deserialize<JiraSearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Issues?.Select(i => new JiraIssue
            {
                Key = i.Key, Summary = i.Fields?.Summary ?? "", Status = i.Fields?.Status?.Name ?? "",
                Priority = i.Fields?.Priority?.Name ?? "", IssueType = i.Fields?.Issuetype?.Name ?? "",
                Created = i.Fields?.Created ?? DateTime.UtcNow, Url = $"{_baseUrl}/browse/{i.Key}"
            }).ToList() ?? new List<JiraIssue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bugs by project");
            return new List<JiraIssue>();
        }
    }

    public async Task<List<JiraIssue>> GetProjectIssuesAsync(int maxResults = 20)
    {
        try
        {
            var url = $"{_baseUrl}/rest/api/3/search?jql=project={_projectKey} AND labels=nexusqa ORDER BY created DESC&maxResults={maxResults}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<JiraIssue>();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JiraSearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Issues?.Select(i => new JiraIssue
            {
                Key = i.Key, Summary = i.Fields?.Summary ?? "", Status = i.Fields?.Status?.Name ?? "",
                Priority = i.Fields?.Priority?.Name ?? "", IssueType = i.Fields?.Issuetype?.Name ?? "",
                Created = i.Fields?.Created ?? DateTime.UtcNow, Url = $"{_baseUrl}/browse/{i.Key}"
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
            if (string.IsNullOrEmpty(issueKey)) return new List<JiraIssue>();
            var bugs = new List<JiraIssue>();
            var r = await _httpClient.GetAsync($"{_baseUrl}/rest/api/3/issue/{issueKey}?fields=subtasks,summary,status,priority,issuetype");
            if (!r.IsSuccessStatusCode) return bugs;
            var rawJson = await r.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("fields", out var fields) || !fields.TryGetProperty("subtasks", out var subtasks))
                return bugs;
            foreach (var subtask in subtasks.EnumerateArray())
            {
                var key = subtask.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(key)) continue;
                var sf = subtask.TryGetProperty("fields", out var f) ? f : default;
                var summary = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined && sf.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
                var status = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined && sf.TryGetProperty("status", out var st) && st.ValueKind != System.Text.Json.JsonValueKind.Null && st.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "";
                var priority = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined && sf.TryGetProperty("priority", out var pr) && pr.ValueKind != System.Text.Json.JsonValueKind.Null && pr.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "";
                var issueType = sf.ValueKind != System.Text.Json.JsonValueKind.Undefined && sf.TryGetProperty("issuetype", out var it) && it.ValueKind != System.Text.Json.JsonValueKind.Null && it.TryGetProperty("name", out var itn) ? itn.GetString() ?? "" : "";
var isRelevant = string.IsNullOrEmpty(issueType) || 
    issueType.ToLower().Contains("bug") || 
    issueType.ToLower().Contains("sub") ||
    issueType.ToLower().Contains("task");
if (!isRelevant) continue;                
bugs.Add(new JiraIssue { Key = key, Summary = summary, Status = status, Priority = priority, IssueType = issueType, Assignee = "Ver en Jira", Created = DateTime.UtcNow, Url = $"{_baseUrl}/browse/{key}" });
            }
            return bugs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bugs by issue URL");
            return new List<JiraIssue>();
        }
    }

    public string ExtractProjectKeyFromUrl(string url)
    {
        try
        {
            var patterns = new[] { @"/projects/([A-Z]+)/", @"/projects/([A-Z]+)$", @"/project/([A-Z]+)", @"project=([A-Z]+)" };
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
                if (match.Success) return match.Groups[1].Value;
            }
            return string.Empty;
        }
        catch { return string.Empty; }
    }

    public string ExtractIssueKeyFromUrl(string url)
    {
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, @"/browse/([A-Z0-9]+-[0-9]+)|/issues/([A-Z0-9]+-[0-9]+)");
            if (match.Success) return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return string.Empty;
        }
        catch { return string.Empty; }
    }

    private string ExtractDescription(System.Text.Json.JsonElement fields)
    {
        try
        {
            if (!fields.TryGetProperty("description", out var desc) || desc.ValueKind == System.Text.Json.JsonValueKind.Null)
                return "Sin descripcion";
            if (desc.TryGetProperty("content", out var content))
            {
                var texts = new List<string>();
                foreach (var block in content.EnumerateArray())
                    if (block.TryGetProperty("content", out var inner))
                        foreach (var node in inner.EnumerateArray())
                            if (node.TryGetProperty("text", out var text))
                                texts.Add(text.GetString() ?? "");
                return string.Join(" ", texts).Trim();
            }
            return desc.GetString() ?? "Sin descripcion";
        }
        catch { return "Sin descripcion"; }
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

public class JiraSearchResult { public List<JiraIssueItem>? Issues { get; set; } }
public class JiraIssueItem { public string Key { get; set; } = string.Empty; public JiraIssueFields? Fields { get; set; } }
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
public class JiraAssignee { public string? DisplayName { get; set; } public string? EmailAddress { get; set; } }
public class JiraStatus { public string Name { get; set; } = string.Empty; }
public class JiraPriority { public string Name { get; set; } = string.Empty; }
public class JiraIssueType { public string Name { get; set; } = string.Empty; }
public class JiraCreateResponse { public string Id { get; set; } = string.Empty; public string Key { get; set; } = string.Empty; }
public class JiraProject { public string Id { get; set; } = string.Empty; public string Key { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public class JiraProjectRaw { public string? Id { get; set; } public string? Key { get; set; } public string? Name { get; set; } }