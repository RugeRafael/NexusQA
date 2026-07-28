using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QACopilot.API.Helpers;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;
using System.Text;
using System.Text.Json;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/training")]
[Authorize(Policy = "SeniorOrAdmin")]
public class TrainingController : ControllerBase
{
    private readonly QACopilotDbContext _context;
    private readonly ILogger<TrainingController> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly string[] AllowedExtensions = [".pdf", ".docx", ".doc", ".txt", ".md", ".html"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public TrainingController(
        QACopilotDbContext context,
        ILogger<TrainingController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _scopeFactory = scopeFactory;
    }

    private string AiServiceUrl => _configuration["AIService:BaseUrl"] ?? "http://localhost:8000";

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId = null)
    {
        var query = _context.TrainingDocuments
            .Where(d => d.IsActive)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(d => d.ProjectId == projectId || d.ProjectId == null);

        var docs = await query
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new TrainingDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                Category = d.Category,
                Status = d.Status,
                Description = d.Description,
                FileSizeBytes = d.FileSizeBytes,
                ContentType = d.ContentType,
                UploadedAt = d.UploadedAt,
                UploadedBy = d.UploadedByUser.FullName,
                ProjectId = d.ProjectId,
                ProjectName = d.Project != null ? d.Project.Name : "Global"
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TrainingDocumentDto>>.Ok(docs));
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string category = "other",
        [FromForm] string? description = null,
        [FromForm] Guid? projectId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No se recibió ningún archivo."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(ApiResponse<object>.Fail("El archivo supera el límite de 10MB."));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail($"Extensión no permitida. Use: {string.Join(", ", AllowedExtensions)}"));

        var userId = Guid.Parse(User.FindFirst("uid")!.Value);

        var uploadDir = "C:/NexusQA/QACopilot/uploads/training";
        Directory.CreateDirectory(uploadDir);
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var doc = new TrainingDocument
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            Category = category,
            Status = "Pending",
            Description = description,
            IsActive = true,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = userId,
            ProjectId = projectId
        };

        _context.TrainingDocuments.Add(doc);
        await _context.SaveChangesAsync();

        var docId = doc.Id;
        var aiUrl = AiServiceUrl;
        var scopeFactory = _scopeFactory;
        var httpClient = _httpClient;
        var logger = _logger;
        var ragNamespace = projectId.HasValue ? projectId.Value.ToString() : "global";

        _ = Task.Run(async () =>
        {
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    doc_id = docId.ToString(),
                    file_path = filePath.Replace("\\", "/"),
                    category = ragNamespace
                });
                var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{aiUrl}/training/index", requestContent);

                var newStatus = response.IsSuccessStatusCode ? "Active" : "Error";
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<QACopilotDbContext>();
                var dbDoc = await db.TrainingDocuments.FindAsync(docId);
                if (dbDoc != null)
                {
                    dbDoc.Status = newStatus;
                    await db.SaveChangesAsync();
                }
                logger.LogInformation("RAG indexing for {DocId} [{Namespace}]: {Status}", docId, ragNamespace, newStatus);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling RAG index for {DocId}", docId);
            }
        });

        _logger.LogInformation("Training document uploaded: {FileName} by {UserId} for project {ProjectId}", file.FileName, userId, projectId);

        var projectName = "Global";
        if (projectId.HasValue)
        {
            var project = await _context.Projects.FindAsync(projectId.Value);
            projectName = project?.Name ?? "Proyecto específico";
        }

        return Ok(ApiResponse<TrainingDocumentDto>.Ok(new TrainingDocumentDto
        {
            Id = doc.Id,
            FileName = doc.FileName,
            Category = doc.Category,
            Status = doc.Status,
            Description = doc.Description,
            FileSizeBytes = doc.FileSizeBytes,
            ContentType = doc.ContentType,
            UploadedAt = doc.UploadedAt,
            UploadedBy = userId.ToString(),
            ProjectId = doc.ProjectId,
            ProjectName = projectName
        }, "Documento subido. Indexando en RAG..."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var doc = await _context.TrainingDocuments.FindAsync(id);
        if (doc == null)
            return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));

        doc.IsActive = false;
        await _context.SaveChangesAsync();

        try { await _httpClient.DeleteAsync($"{AiServiceUrl}/training/index/{id}"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error eliminando del índice RAG: {DocId}", id); }

        if (System.IO.File.Exists(doc.FilePath))
            System.IO.File.Delete(doc.FilePath);

        return Ok(ApiResponse<object>.Ok(null, "Documento eliminado."));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var doc = await _context.TrainingDocuments.FindAsync(id);
        if (doc == null)
            return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));

        doc.Status = request.Status;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Estado actualizado."));
    }

    [HttpGet("rag/stats")]
    public async Task<IActionResult> GetRagStats()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{AiServiceUrl}/training/stats");
            var content = await response.Content.ReadAsStringAsync();
            return Ok(ApiResponse<object>.Ok(JsonSerializer.Deserialize<object>(content)));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object>.Ok(new { status = "unavailable", error = ex.Message }));
        }
    }
}

public class TrainingDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string ProjectName { get; set; } = "Global";
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
