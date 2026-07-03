using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Documents;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

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

    [HttpGet]
    public async Task<IActionResult> GetMyDocuments()
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _documentService.GetByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<DocumentResponseDto>>.Ok(result));
    }
}