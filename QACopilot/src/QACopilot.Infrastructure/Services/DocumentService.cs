using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Documents;
using QACopilot.Application.Interfaces;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Domain.Enums;

namespace QACopilot.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentService> _logger;
    private const string BaseUploadPath = "C:/NexusQA/QACopilot/uploads/documents";

    public DocumentService(IUnitOfWork unitOfWork, ILogger<DocumentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DocumentResponseDto> UploadAsync(UploadDocumentDto request, Guid userId)
    {
        _logger.LogInformation("Uploading document {FileName} for user {UserId}", request.FileName, userId);

        // Guardar archivo en disco
        var userDir = Path.Combine(BaseUploadPath, userId.ToString());
        Directory.CreateDirectory(userDir);
        var filePath = Path.Combine(userDir, request.FileName);

        if (request.FileContent != null && request.FileContent.Length > 0)
        {
            await File.WriteAllBytesAsync(filePath, request.FileContent);
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            FilePath = filePath.Replace("\\", "/"),
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            Status = DocumentStatus.Uploaded.ToString(),
            UserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.Documents.AddAsync(document);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} uploaded successfully to {FilePath}", document.Id, filePath);
        return MapToDto(document);
    }

    public async Task<IEnumerable<DocumentResponseDto>> GetByUserAsync(Guid userId)
    {
        var documents = await _unitOfWork.Documents.GetByUserIdAsync(userId);
        return documents.Select(MapToDto);
    }

    private static DocumentResponseDto MapToDto(Document doc) => new()
    {
        Id = doc.Id,
        FileName = doc.FileName,
        ContentType = doc.ContentType,
        FileSizeBytes = doc.FileSizeBytes,
        Status = doc.Status,
        UploadedAt = doc.UploadedAt
    };
}
