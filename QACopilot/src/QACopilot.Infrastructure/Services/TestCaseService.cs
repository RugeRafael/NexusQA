using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.TestCases;
using QACopilot.Application.Interfaces;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Domain.Enums;
using QACopilot.Domain.Exceptions;

namespace QACopilot.Infrastructure.Services;

public class TestCaseService : ITestCaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;
    private readonly ILogger<TestCaseService> _logger;

    public TestCaseService(IUnitOfWork unitOfWork, IAIService aiService, ILogger<TestCaseService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<TestCaseResponseDto> GenerateAsync(GenerateTestCaseDto request, Guid userId)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(request.DocumentId)
            ?? throw new NotFoundException("Document", request.DocumentId);

        _logger.LogInformation("Generating test cases for document {DocumentId}", request.DocumentId);

        // Leer contenido real del archivo
        var documentContent = await ReadDocumentContentAsync(document);
        if (request.AdditionalContext is not null)
            documentContent = $"{documentContent}\n\nContexto adicional: {request.AdditionalContext}";

        // Pasar projectId si viene en el request
        var projectId = request.ProjectId?.ToString() ?? "global";
        var aiResult = await _aiService.GenerateTestCasesAsync(documentContent, projectId);

        var history = new TestCaseHistory
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            UserId = userId,
            GeneratedContent = aiResult.Content,
            TotalTestCases = aiResult.TotalTestCases,
            ConfidenceScore = aiResult.ConfidenceScore,
            Status = TestCaseStatus.Completed.ToString(),
            GeneratedAt = DateTime.UtcNow
        };

        await _unitOfWork.TestCaseHistories.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} test cases for document {DocumentId}",
            history.TotalTestCases, request.DocumentId);

        return new TestCaseResponseDto
        {
            Id = history.Id,
            GeneratedContent = history.GeneratedContent,
            TotalTestCases = history.TotalTestCases,
            ConfidenceScore = history.ConfidenceScore,
            GeneratedAt = history.GeneratedAt
        };
    }

    private async Task<string> ReadDocumentContentAsync(Document document)
    {
        try
        {
            var filePath = document.FilePath;

            // Intentar ruta absoluta primero
            if (File.Exists(filePath))
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".txt" || ext == ".md" || ext == ".html")
                    return await File.ReadAllTextAsync(filePath);

                if (ext == ".pdf")
                    return await ExtractPdfTextAsync(filePath);

                if (ext == ".docx" || ext == ".doc")
                    return await ExtractDocxTextAsync(filePath);

                // Para otros tipos, retornar nombre del archivo
                return $"Documento: {document.FileName}";
            }

            // Intentar ruta relativa desde el proyecto
            var altPath = Path.Combine("C:/NexusQA/QACopilot/uploads/documents",
                Path.GetFileName(filePath));
            if (File.Exists(altPath))
                return await File.ReadAllTextAsync(altPath);

            _logger.LogWarning("Document file not found: {FilePath}", filePath);
            return $"Documento: {document.FileName} (archivo no encontrado en disco)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading document {DocumentId}", document.Id);
            return $"Documento: {document.FileName}";
        }
    }

    private static async Task<string> ExtractPdfTextAsync(string filePath)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            using var doc = UglyToad.PdfPig.PdfDocument.Open(filePath);
            foreach (var page in doc.GetPages())
            {
                foreach (var word in page.GetWords())
                    sb.Append(word.Text).Append(' ');
                sb.AppendLine();
            }
            var result = sb.ToString().Trim();
            return result.Length > 50 ? result[..Math.Min(result.Length, 12000)] : $"PDF: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            return $"PDF: {Path.GetFileName(filePath)} (error: {ex.Message})";
        }
    }

    private static Task<string> ExtractDocxTextAsync(string filePath)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    sb.AppendLine(para.InnerText);
                }
            }
            var result = sb.ToString().Trim();
            return Task.FromResult(result.Length > 50
                ? result[..Math.Min(result.Length, 12000)]
                : $"DOCX: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"DOCX: {Path.GetFileName(filePath)} (error: {ex.Message})");
        }
    }

    public async Task<PagedResultDto<TestCaseHistoryDto>> GetHistoryAsync(int page, int pageSize, Guid userId)
    {
        var (items, total) = await _unitOfWork.TestCaseHistories.GetPagedAsync(page, pageSize, userId);
        return new PagedResultDto<TestCaseHistoryDto>
        {
            Items = items.Select(t => new TestCaseHistoryDto
            {
                Id = t.Id,
                DocumentName = t.Document?.FileName ?? string.Empty,
                TotalTestCases = t.TotalTestCases,
                ConfidenceScore = t.ConfidenceScore,
                Status = t.Status,
                GeneratedAt = t.GeneratedAt
            }),
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
