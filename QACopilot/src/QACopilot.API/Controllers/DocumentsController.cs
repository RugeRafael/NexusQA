using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Documents;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly QACopilotDbContext _context;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        QACopilotDbContext context,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _context = context;
        _logger = logger;
    }

    // POST /api/documents/upload
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? description)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var dto = new UploadDocumentDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            FileContent = memoryStream.ToArray(),
            Description = description ?? string.Empty
        };

        var result = await _documentService.UploadAsync(dto, userId);
        return Ok(ApiResponse<DocumentResponseDto>.Ok(result, "Document uploaded successfully."));
    }

    // GET /api/documents — solo del usuario actual
    [HttpGet]
    public async Task<IActionResult> GetMyDocuments()
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _documentService.GetByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<DocumentResponseDto>>.Ok(result));
    }

    // GET /api/documents/all — Admin/Senior ve todos con info de usuario
    [HttpGet("all")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> GetAllDocuments(
        [FromQuery] Guid? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Documents
            .Include(d => d.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(d => d.UserId == userId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                id = d.Id,
                fileName = d.FileName,
                contentType = d.ContentType,
                fileSizeBytes = d.FileSizeBytes,
                status = d.Status,
                uploadedAt = d.UploadedAt,
                userName = d.User != null ? d.User.FullName : "Unknown",
                userEmail = d.User != null ? d.User.Email : ""
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }
}
