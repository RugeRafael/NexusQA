using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Reports;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReportService> _logger;
    private readonly string _aiServiceUrl;

    public ReportService(HttpClient httpClient, IConfiguration config, ILogger<ReportService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceUrl = config["AIService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<ReportResponseDto> GenerateComparisonReportAsync(ReportRequestDto request)
    {
        request.ReportType = "comparison";
        return await CallAIServiceAsync(request, "Informe — Requerimientos vs. Plan de Pruebas");
    }

    public async Task<ReportResponseDto> GenerateCompletionReportAsync(ReportRequestDto request)
    {
        request.ReportType = "completion";
        return await CallAIServiceAsync(request, "Informe de Finalización — Plan de Pruebas");
    }

    public async Task<ReportResponseDto> GenerateInnovationReportAsync(ReportRequestDto request)
    {
        request.ReportType = "innovation";
        return await CallAIServiceAsync(request, "Informe de Innovación QA");
    }

    private async Task<ReportResponseDto> CallAIServiceAsync(ReportRequestDto request, string title)
    {
        try
        {
            var form = new MultipartFormDataContent();

            form.Add(new StringContent(request.ReportType), "report_type");
            form.Add(new StringContent(request.ProjectName ?? ""), "project_name");
            form.Add(new StringContent(request.QAEngineer ?? ""), "qa_engineer");
            form.Add(new StringContent(request.Version ?? "1.0"), "version");
            form.Add(new StringContent(request.Period ?? ""), "period");
            form.Add(new StringContent(request.AdditionalContext ?? ""), "additional_context");
            form.Add(new StringContent(JsonSerializer.Serialize(request.Requirements ?? new())), "requirements");
            form.Add(new StringContent(JsonSerializer.Serialize(request.TestCases ?? new())), "test_cases");
            form.Add(new StringContent(JsonSerializer.Serialize(request.Defects ?? new())), "defects");
            form.Add(new StringContent(request.TotalTestCases.ToString()), "total_test_cases");
            form.Add(new StringContent(request.PassedTestCases.ToString()), "passed_test_cases");
            form.Add(new StringContent(request.FailedTestCases.ToString()), "failed_test_cases");
            form.Add(new StringContent(request.BlockedTestCases.ToString()), "blocked_test_cases");
            form.Add(new StringContent(request.TotalExecutionTimeMinutes.ToString()), "total_execution_time");
            form.Add(new StringContent(request.JiraBugsRaw ?? "[]"), "jira_bugs");


            if (request.DocumentContent != null && request.DocumentContent.Length > 0)
            {
                var fileContent = new ByteArrayContent(request.DocumentContent);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    request.DocumentContentType ?? "application/octet-stream");
                form.Add(fileContent, "document", request.DocumentFileName ?? "document");
            }

            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/api/generate-report", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI service error: {Status} — {Body}", response.StatusCode, body);
                return new ReportResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Error del microservicio IA: {body}"
                };
            }

            var result = JsonSerializer.Deserialize<AIReportResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new ReportResponseDto
            {
                ReportType = request.ReportType,
                Title = result?.Title ?? title,
                HtmlContent = result?.HtmlContent ?? "",
                ProjectName = request.ProjectName,
                QAEngineer = request.QAEngineer,
                Version = request.Version,
                Period = request.Period,
                GeneratedAt = DateTime.UtcNow,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI service for report");
            return new ReportResponseDto { Success = false, ErrorMessage = ex.Message };
        }
    }
}

public class AIReportResponse
{
    public bool Success { get; set; }
    public string HtmlContent { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}