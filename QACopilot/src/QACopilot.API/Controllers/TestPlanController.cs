using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;
using QACopilot.Infrastructure.Services.ExternalServices;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/testplan")]
[Authorize]
public class TestPlanController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly QACopilotDbContext _context;
    private readonly ILogger<TestPlanController> _logger;

    public TestPlanController(IAIService aiService, QACopilotDbContext context, ILogger<TestPlanController> logger)
    {
        _aiService = aiService;
        _context = context;
        _logger = logger;
    }

    // POST /api/testplan/analyze
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeTestPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlanContent) || request.PlanContent.Length < 50)
            return BadRequest(ApiResponse<object>.Fail("El plan debe tener al menos 50 caracteres"));

        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var startTime = DateTime.UtcNow;

        try
        {
            var aiServiceConcrete = _aiService as AIService;
            if (aiServiceConcrete == null)
                return StatusCode(503, ApiResponse<object>.Fail("Servicio IA no disponible"));

            var result = await aiServiceConcrete.AnalyzeTestPlanAsync(request.PlanContent, request.ProjectName ?? "");

            var analysis = new TestPlanAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = request.ProjectName ?? "Plan de Pruebas",
                ProjectName = request.ProjectName ?? "",
                FilePath = "",
                IsViable = result.IsViable,
                ViabilityReason = result.ViabilityReason,
                IstqbComplianceNotes = result.IstqbComplianceNotes,
                Iso29119ComplianceNotes = result.Iso29119ComplianceNotes,
                EstimatedTimeJson = result.EstimatedTimeJson,
                AIAnalysisResult = result.AiAnalysisResult,
                ConfidenceScore = result.ConfidenceScore,
                Status = result.IsViable ? "Viable" : "NotViable",
                UploadedAt = DateTime.UtcNow,
                AnalyzedAt = DateTime.UtcNow
            };

            await _context.TestPlanAnalyses.AddAsync(analysis);
            await _context.AuditMetrics.AddAsync(new AuditMetric
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Module = "TestPlan",
                Action = "AnalyzeTestPlan",
                Success = true,
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                OccurredAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                is_viable = result.IsViable,
                viability_reason = result.ViabilityReason,
                istqb_compliance_notes = result.IstqbComplianceNotes,
                iso29119_compliance_notes = result.Iso29119ComplianceNotes,
                estimated_time_json = result.EstimatedTimeJson,
                ai_analysis_result = result.AiAnalysisResult,
                confidence_score = result.ConfidenceScore,
                model_used = result.ModelUsed,
                analysis_id = analysis.Id
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing test plan");
            return StatusCode(500, ApiResponse<object>.Fail("Error al analizar el plan"));
        }
    }

    // POST /api/testplan/{id}/save-report
    [HttpPost("{id}/save-report")]
    public async Task<IActionResult> SaveReport(Guid id, [FromBody] SaveReportRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var analysis = await _context.TestPlanAnalyses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (analysis == null)
            return NotFound(ApiResponse<object>.Fail("Análisis no encontrado"));

        analysis.ReportHtml = request.HtmlContent;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { saved = true }));
    }

    // GET /api/testplan/{id}/download-report
    [HttpGet("{id}/download-report")]
    public async Task<IActionResult> DownloadReport(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var analysis = await _context.TestPlanAnalyses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (analysis == null)
            return NotFound(ApiResponse<object>.Fail("Análisis no encontrado"));

        if (string.IsNullOrEmpty(analysis.ReportHtml))
            return NotFound(ApiResponse<object>.Fail("Reporte no generado aún"));

        var bytes = System.Text.Encoding.UTF8.GetBytes(analysis.ReportHtml);
        var fileName = $"Analisis_{analysis.FileName}_{analysis.AnalyzedAt:yyyyMMdd}.html";
        return File(bytes, "text/html", fileName);
    }

    // GET /api/testplan/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var analyses = await _context.TestPlanAnalyses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AnalyzedAt)
            .Take(20)
            .Select(a => new
            {
                id = a.Id,
                fileName = a.FileName,
                projectName = a.ProjectName,
                isViable = a.IsViable,
                confidenceScore = a.ConfidenceScore,
                status = a.Status,
                analyzedAt = a.AnalyzedAt,
                hasReport = !string.IsNullOrEmpty(a.ReportHtml)
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(analyses));
    }
}

public class AnalyzeTestPlanRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("plan_content")]
    public string PlanContent { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }
}

public class SaveReportRequest
{
    public string HtmlContent { get; set; } = string.Empty;
}
