using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Reports;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpPost("comparison")]
    public async Task<IActionResult> GenerateComparison([FromForm] ReportFormRequest form)
    {
        var request = await BuildRequest(form, "comparison");
        var result = await _reportService.GenerateComparisonReportAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error generando informe"));
        return Ok(ApiResponse<ReportResponseDto>.Ok(result));
    }

    [HttpPost("completion")]
    public async Task<IActionResult> GenerateCompletion([FromForm] ReportFormRequest form)
    {
        var request = await BuildRequest(form, "completion");
        var result = await _reportService.GenerateCompletionReportAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error generando informe"));
        return Ok(ApiResponse<ReportResponseDto>.Ok(result));
    }

    [HttpPost("innovation")]
    public async Task<IActionResult> GenerateInnovation([FromForm] ReportFormRequest form)
    {
        var request = await BuildRequest(form, "innovation");
        var result = await _reportService.GenerateInnovationReportAsync(request);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Error generando informe"));
        return Ok(ApiResponse<ReportResponseDto>.Ok(result));
    }

    private async Task<ReportRequestDto> BuildRequest(ReportFormRequest form, string type)
    {
          // Agrega este log temporal
    _logger.LogInformation("JiraBugs raw from form: {Bugs}", form.JiraBugs ?? "NULL");
        var request = new ReportRequestDto
        {
            ReportType = type,
            ProjectName = form.ProjectName,
            QAEngineer = form.QAEngineer,
            Version = form.Version ?? "1.0",
            Period = form.Period ?? "",
            AdditionalContext = form.AdditionalContext ?? "",
            TotalTestCases = form.TotalTestCases,
            PassedTestCases = form.PassedTestCases,
            FailedTestCases = form.FailedTestCases,
            BlockedTestCases = form.BlockedTestCases,
            TotalExecutionTimeMinutes = form.TotalExecutionTimeMinutes,
            Requirements = form.Requirements?.Split('\n')
                .Where(r => !string.IsNullOrWhiteSpace(r)).ToList() ?? new(),
            TestCases = form.TestCases?.Split('\n')
                .Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new(),
            Defects = form.Defects?.Split('\n')
                .Where(d => !string.IsNullOrWhiteSpace(d)).ToList() ?? new()
                
        };
request.JiraBugsRaw = form.JiraBugs ?? "[]";
        if (form.Document != null && form.Document.Length > 0)
        {
            using var ms = new MemoryStream();
            await form.Document.CopyToAsync(ms);
            request.DocumentContent = ms.ToArray();
            request.DocumentFileName = form.Document.FileName;
            request.DocumentContentType = form.Document.ContentType;
        }

        return request;
    }
}

public class ReportFormRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string QAEngineer { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Period { get; set; }
    public string? AdditionalContext { get; set; }
    public string? Requirements { get; set; }
    public string? TestCases { get; set; }
    public string? Defects { get; set; }
    public int TotalTestCases { get; set; }
    public int PassedTestCases { get; set; }
    public int FailedTestCases { get; set; }
    public int BlockedTestCases { get; set; }
    public double TotalExecutionTimeMinutes { get; set; }
    public IFormFile? Document { get; set; }
      public string? JiraBugs { get; set; }  // recibir como string raw
}