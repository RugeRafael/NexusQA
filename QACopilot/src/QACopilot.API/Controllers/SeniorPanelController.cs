using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/senior-panel")]
[Authorize]
public class SeniorPanelController : ControllerBase
{
    private readonly QACopilotDbContext _context;
    private readonly ILogger<SeniorPanelController> _logger;

    private const double EFFECTIVE_HOURS_PER_DAY = 7.0;
    private const int META_DOCUMENTOS_SPRINT = 3;

    public SeniorPanelController(QACopilotDbContext context, ILogger<SeniorPanelController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("team")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> GetTeamPanel([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var fechaInicio = year.HasValue && month.HasValue
            ? new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc)
            : (DateTime?)null;
        var fechaFin = fechaInicio?.AddMonths(1).AddSeconds(-1);

        var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
        var result = new List<QAIndicatorDto>();

        foreach (var user in users)
        {
            var indicator = await CalculateIndicators(user.Id, fechaInicio, fechaFin);
            indicator.UserId = user.Id;
            indicator.UserName = user.FullName;
            indicator.Email = user.Email;
            indicator.Role = user.Role;
            result.Add(indicator);
        }

        return Ok(ApiResponse<List<QAIndicatorDto>>.Ok(result));
    }

    [HttpGet("my-indicators")]
    public async Task<IActionResult> GetMyIndicators([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var config = await _context.SeniorPanelConfigs.FirstOrDefaultAsync(c => c.UserId == userId);
        if (config == null || !config.IndicatorsEnabled)
            return Ok(ApiResponse<object>.Ok(new { enabled = false }));

        var fechaInicio = year.HasValue && month.HasValue
            ? new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc)
            : (DateTime?)null;
        var fechaFin = fechaInicio?.AddMonths(1).AddSeconds(-1);

        var indicator = await CalculateIndicators(userId, fechaInicio, fechaFin);
        indicator.UserId = userId;
        indicator.UserName = user.FullName;
        indicator.Email = user.Email;
        indicator.Role = user.Role;

        return Ok(ApiResponse<QAIndicatorDto>.Ok(indicator));
    }

    [HttpGet("config")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> GetConfigs()
    {
        var configs = await _context.SeniorPanelConfigs.Include(c => c.User).ToListAsync();
        var result = configs.Select(c => new SeniorPanelConfigDto
        {
            UserId = c.UserId,
            UserName = c.User?.FullName ?? "",
            Email = c.User?.Email ?? "",
            IndicatorsEnabled = c.IndicatorsEnabled,
            MetaDocumentos = c.MetaDocumentos,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return Ok(ApiResponse<List<SeniorPanelConfigDto>>.Ok(result));
    }

    [HttpPost("config")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateSeniorConfigRequest request)
    {
        var config = await _context.SeniorPanelConfigs.FirstOrDefaultAsync(c => c.UserId == request.UserId);
        if (config == null)
        {
            config = new SeniorPanelConfig
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                IndicatorsEnabled = request.IndicatorsEnabled,
                MetaDocumentos = request.MetaDocumentos ?? META_DOCUMENTOS_SPRINT,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.SeniorPanelConfigs.AddAsync(config);
        }
        else
        {
            config.IndicatorsEnabled = request.IndicatorsEnabled;
            config.MetaDocumentos = request.MetaDocumentos ?? config.MetaDocumentos;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { updated = true }));
    }

    [HttpGet("user/{userId}")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> GetUserIndicators(Guid userId, [FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var fechaInicio = year.HasValue && month.HasValue
            ? new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc)
            : (DateTime?)null;
        var fechaFin = fechaInicio?.AddMonths(1).AddSeconds(-1);

        var indicator = await CalculateIndicators(userId, fechaInicio, fechaFin);
        indicator.UserId = userId;
        indicator.UserName = user.FullName;
        indicator.Email = user.Email;
        indicator.Role = user.Role;

        return Ok(ApiResponse<QAIndicatorDto>.Ok(indicator));
    }

    private async Task<QAIndicatorDto> CalculateIndicators(Guid userId, DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        var config = await _context.SeniorPanelConfigs.FirstOrDefaultAsync(c => c.UserId == userId);
        var metaDocs = config?.MetaDocumentos ?? META_DOCUMENTOS_SPRINT;

        var proyectosAsignados = await _context.ProjectAssignments
            .Where(pa => pa.UserId == userId && pa.IsActive)
            .Select(pa => pa.ProjectId).ToListAsync();

        var documentosProyecto = await _context.Documents.Where(d => d.UserId == userId).ToListAsync();

        var historialQuery = _context.TestCaseHistories.Where(t => t.UserId == userId);
        if (fechaInicio.HasValue) historialQuery = historialQuery.Where(t => t.GeneratedAt >= fechaInicio.Value);
        if (fechaFin.HasValue) historialQuery = historialQuery.Where(t => t.GeneratedAt <= fechaFin.Value);
        var historial = await historialQuery.OrderByDescending(t => t.GeneratedAt).Take(10).ToListAsync();

        var totalCPs = historial.Sum(t => t.TotalTestCases);
        var totalDias = historial.Any() ? (DateTime.UtcNow - historial.Min(t => t.GeneratedAt)).TotalDays : 1;
        var horasEfectivas = Math.Max(totalDias * EFFECTIVE_HOURS_PER_DAY, 1);
        var eficienciaTemporal = totalCPs > 0 ? Math.Min(totalCPs / horasEfectivas * 10, 100) : 0;
        var scorePlan = historial.Any() ? Math.Min((eficienciaTemporal + (totalCPs > 0 ? 80 : 0)) / 2, 100) : 0;

        var docsQuery = _context.Documents.Where(d => d.UserId == userId);
        if (fechaInicio.HasValue) docsQuery = docsQuery.Where(d => d.UploadedAt >= fechaInicio.Value);
        if (fechaFin.HasValue) docsQuery = docsQuery.Where(d => d.UploadedAt <= fechaFin.Value);
        var totalDocs = await docsQuery.CountAsync();
        var scoreDocumental = Math.Min((double)totalDocs / metaDocs * 100, 100);

        var aportesQuery = _context.AuditMetrics.Where(a => a.UserId == userId &&
            (a.Action.Contains("Report") || a.Action.Contains("Chat") ||
             a.Action.Contains("Document") || a.Action.Contains("TestCase")));
        if (fechaInicio.HasValue) aportesQuery = aportesQuery.Where(a => a.OccurredAt >= fechaInicio.Value);
        if (fechaFin.HasValue) aportesQuery = aportesQuery.Where(a => a.OccurredAt <= fechaFin.Value);
        var totalAportes = await aportesQuery.CountAsync();
        var scoreAportes = Math.Min(totalAportes * 10.0, 100);

        var scoreFinal = (scorePlan * 0.50) + (scoreDocumental * 0.25) + (scoreAportes * 0.25);

        return new QAIndicatorDto
        {
            ScorePlan = Math.Round(scorePlan, 1), TotalCPs = totalCPs,
            HorasEfectivas = Math.Round(horasEfectivas, 1), EficienciaTemporal = Math.Round(eficienciaTemporal, 1),
            ScoreDocumental = Math.Round(scoreDocumental, 1), TotalDocumentos = totalDocs,
            MetaDocumentos = metaDocs, DocumentosEnProyectos = documentosProyecto.Count,
            ScoreAportes = Math.Round(scoreAportes, 1), TotalAportes = totalAportes,
            ScoreFinal = Math.Round(scoreFinal, 1), ProyectosAsignados = proyectosAsignados.Count,
            IndicatorsEnabled = config?.IndicatorsEnabled ?? false, MetaDocumentosConfig = metaDocs
        };
    }
}

public class QAIndicatorDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public double ScorePlan { get; set; }
    public int TotalCPs { get; set; }
    public double HorasEfectivas { get; set; }
    public double EficienciaTemporal { get; set; }
    public double ScoreDocumental { get; set; }
    public int TotalDocumentos { get; set; }
    public int MetaDocumentos { get; set; }
    public int DocumentosEnProyectos { get; set; }
    public double ScoreAportes { get; set; }
    public int TotalAportes { get; set; }
    public double ScoreFinal { get; set; }
    public int ProyectosAsignados { get; set; }
    public bool IndicatorsEnabled { get; set; }
    public int MetaDocumentosConfig { get; set; }
}

public class SeniorPanelConfigDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IndicatorsEnabled { get; set; }
    public int MetaDocumentos { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSeniorConfigRequest
{
    public Guid UserId { get; set; }
    public bool IndicatorsEnabled { get; set; }
    public int? MetaDocumentos { get; set; }
}
