using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;
using QACopilot.Infrastructure.Services.ExternalServices;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/testplan")]
[Authorize]
public class TestPlanController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly QACopilotDbContext _context;
    private readonly ILogger<TestPlanController> _logger;

    public TestPlanController(
        IAIService aiService,
        QACopilotDbContext context,
        ILogger<TestPlanController> logger)
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
            _logger.LogInformation("Analyzing test plan for project: {Project}", request.ProjectName);

            var aiServiceConcrete = _aiService as AIService;
            if (aiServiceConcrete == null)
                return StatusCode(503, ApiResponse<object>.Fail("Servicio IA no disponible"));

            var result = await aiServiceConcrete.AnalyzeTestPlanAsync(
                request.PlanContent, request.ProjectName ?? "");

            // Guardar en historial usando campos correctos de la entidad
            var analysis = new TestPlanAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = request.ProjectName ?? "Plan de Pruebas",
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

            // AuditMetrics
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
            await _context.AuditMetrics.AddAsync(new AuditMetric
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Module = "TestPlan",
                Action = "AnalyzeTestPlan",
                Success = false,
                ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)],
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                OccurredAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return StatusCode(500, ApiResponse<object>.Fail("Error al analizar el plan"));
        }
    }

    // POST /api/testplan/analyze-file
    [HttpPost("analyze-file")]
    public async Task<IActionResult> AnalyzeFile(IFormFile file, [FromForm] string? projectName = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Archivo requerido"));

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();

        return await Analyze(new AnalyzeTestPlanRequest
        {
            PlanContent = content,
            ProjectName = projectName ?? file.FileName
        });
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
                isViable = a.IsViable,
                confidenceScore = a.ConfidenceScore,
                status = a.Status,
                analyzedAt = a.AnalyzedAt
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

