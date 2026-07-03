using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.TestCases;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.Infrastructure.Services.ExternalServices;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly string _aiServiceUrl;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AIService(HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceUrl = configuration["AIService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<AIGenerationResultDto> GenerateTestCasesAsync(string documentContent)
    {
        try
        {
            _logger.LogInformation("Calling AI microservice at {Url}", _aiServiceUrl);
            var payload = new { document_content = documentContent };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/generate-testcases", content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("AI raw response: {Response}", responseBody[..Math.Min(200, responseBody.Length)]);
            var result = JsonSerializer.Deserialize<AIGenerationResultDto>(responseBody, _jsonOptions);
            _logger.LogInformation("AI microservice returned {Count} test cases with score {Score}",
                result?.TotalTestCases, result?.ConfidenceScore);
            return result ?? new AIGenerationResultDto
            {
                Content = "No content generated",
                TotalTestCases = 0,
                ConfidenceScore = 0
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI microservice unavailable");
            return new AIGenerationResultDto
            {
                Content = "AI service temporarily unavailable. Please try again later.",
                TotalTestCases = 0,
                ConfidenceScore = 0
            };
        }
    }

    public async Task<string> ChatAsync(string message, List<Dictionary<string, string>>? sessionHistory = null)
    {
        try
        {
            _logger.LogInformation("Chat request to AI microservice: {Msg}", message[..Math.Min(80, message.Length)]);
            var payload = new
            {
                message,
                session_history = sessionHistory ?? new List<Dictionary<string, string>>()
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/chat", content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Chat response received: {Length} chars", responseBody.Length);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("response", out var responseEl))
                return responseEl.GetString() ?? "Sin respuesta";
            if (root.TryGetProperty("content", out var contentEl))
                return contentEl.GetString() ?? "Sin respuesta";
            return responseBody;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI chat microservice unavailable");
            return "El servicio de IA no está disponible en este momento. Por favor intenta de nuevo.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatAsync");
            return "Ocurrió un error al procesar tu consulta. Por favor intenta de nuevo.";
        }
    }

    public async Task<TestPlanAnalysisResultDto> AnalyzeTestPlanAsync(string planContent, string projectName = "")
    {
        try
        {
            _logger.LogInformation("Analyzing test plan for project: {Project}", projectName);
            var payload = new { plan_content = planContent, project_name = projectName };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/analyze-testplan", content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestPlanAnalysisResultDto>(responseBody, _jsonOptions);
            return result ?? new TestPlanAnalysisResultDto { IsViable = false, ConfidenceScore = 0 };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI test plan microservice unavailable");
            return new TestPlanAnalysisResultDto
            {
                IsViable = false,
                ViabilityReason = "Servicio de IA no disponible",
                ConfidenceScore = 0
            };
        }
    }
}

public class TestPlanAnalysisResultDto
{
    public bool IsViable { get; set; }
    public string? ViabilityReason { get; set; }
    public string? IstqbComplianceNotes { get; set; }
    public string? Iso29119ComplianceNotes { get; set; }
    public string? EstimatedTimeJson { get; set; }
    public string? AiAnalysisResult { get; set; }
    public double ConfidenceScore { get; set; }
    public string? ModelUsed { get; set; }
}
