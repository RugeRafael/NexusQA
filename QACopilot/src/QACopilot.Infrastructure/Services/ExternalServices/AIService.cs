using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.TestCases;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Exceptions;

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

    public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceUrl = configuration["AIService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<AIGenerationResultDto> GenerateTestCasesAsync(string documentContent, string? projectId = null)
    {
        try
        {
            _logger.LogInformation("Calling AI microservice at {Url} for project {ProjectId}", _aiServiceUrl, projectId ?? "global");

            var payload = new
            {
                document_content = documentContent,
                project_id = projectId ?? "global"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/generate-testcases", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ThrowMappedException(response.StatusCode, responseBody);
            }

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
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AI microservice timed out");
            throw new AiServiceException(
                "AI_TIMEOUT",
                "La IA tardó demasiado en responder. Intenta con un documento más corto o vuelve a intentarlo.",
                HttpStatusCode.GatewayTimeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI microservice unreachable");
            throw new AiServiceException(
                "AI_UNAVAILABLE",
                "El servicio de IA no está disponible en este momento. Intenta de nuevo más tarde.",
                HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<string> ChatAsync(string message, List<Dictionary<string, string>>? sessionHistory = null, string? projectId = null, string? projectName = null)
    {
        try
        {
            _logger.LogInformation("Chat request to AI microservice: {Msg}", message[..Math.Min(80, message.Length)]);

            var payload = new
            {
                message,
                session_history = sessionHistory ?? new List<Dictionary<string, string>>(),
                project_id = projectId ?? "global",
                project_name = projectName
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/chat", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var lower = responseBody.ToLowerInvariant();
                if (lower.Contains("rate_limit") || lower.Contains("429") || lower.Contains("too many requests"))
                {
                    _logger.LogWarning("AI rate limit hit in chat: {Body}", responseBody);
                    return "El servicio de IA está muy solicitado en este momento (límite de uso alcanzado). Intenta de nuevo en unos segundos.";
                }

                _logger.LogError("AI chat service returned error {Status}: {Body}", response.StatusCode, responseBody);
                return "El servicio de IA no está disponible en este momento. Por favor intenta de nuevo.";
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("response", out var responseEl))
                return responseEl.GetString() ?? "Sin respuesta";
            if (root.TryGetProperty("content", out var contentEl))
                return contentEl.GetString() ?? "Sin respuesta";

            return responseBody;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AI chat microservice timed out");
            return "La IA tardó demasiado en responder. Intenta de nuevo en unos segundos.";
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
        _logger.LogInformation("Analyzing test plan for project: {Project}", projectName);

        HttpResponseMessage response;
        try
        {
            var payload = new { plan_content = planContent, project_name = projectName };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/analyze-testplan", content);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AI test plan microservice timed out");
            throw new AiServiceException(
                "AI_TIMEOUT",
                "La IA tardó demasiado en responder. Intenta con un documento más corto o vuelve a intentarlo.",
                HttpStatusCode.GatewayTimeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI test plan microservice unreachable");
            throw new AiServiceException(
                "AI_UNAVAILABLE",
                "El servicio de IA no está disponible en este momento. Intenta de nuevo más tarde.",
                HttpStatusCode.ServiceUnavailable);
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ThrowMappedException(response.StatusCode, responseBody);
        }

        var result = JsonSerializer.Deserialize<TestPlanAnalysisResultDto>(responseBody, _jsonOptions);
        return result ?? new TestPlanAnalysisResultDto { IsViable = false, ConfidenceScore = 0 };
    }

    /// <summary>
    /// Inspecciona el body de error devuelto por el microservicio Python y lanza
    /// una excepcion con el ErrorCode correcto (rate limit vs. fallo generico).
    /// </summary>
    private static void ThrowMappedException(HttpStatusCode statusCode, string responseBody)
    {
        var lower = responseBody.ToLowerInvariant();
        var isRateLimit = lower.Contains("rate_limit") || lower.Contains("429") || lower.Contains("too many requests");

        if (isRateLimit)
        {
            throw new AiServiceException(
                "AI_RATE_LIMIT",
                "El documento es demasiado grande para procesarlo en este momento (límite de la IA alcanzado). Intenta con un documento más corto o espera unos minutos.",
                HttpStatusCode.TooManyRequests);
        }

        throw new AiServiceException(
            "AI_UNAVAILABLE",
            "El servicio de IA no está disponible en este momento. Intenta de nuevo más tarde.",
            HttpStatusCode.ServiceUnavailable);
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
