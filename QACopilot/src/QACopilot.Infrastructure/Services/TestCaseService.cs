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

    public TestCaseService(
        IUnitOfWork unitOfWork,
        IAIService aiService,
        ILogger<TestCaseService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<TestCaseResponseDto> GenerateAsync(
        GenerateTestCaseDto request, Guid userId)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(request.DocumentId)
            ?? throw new NotFoundException("Document", request.DocumentId);

        _logger.LogInformation(
            "Generating test cases for document {DocumentId}", request.DocumentId);

        var documentContent = request.AdditionalContext is not null
            ? $"{document.FileName}\n{request.AdditionalContext}"
            : document.FileName;

        var aiResult = await _aiService.GenerateTestCasesAsync(documentContent);

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

        _logger.LogInformation(
            "Generated {Count} test cases for document {DocumentId}",
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

    public async Task<PagedResultDto<TestCaseHistoryDto>> GetHistoryAsync(
        int page, int pageSize)
    {
        var (items, total) = await _unitOfWork.TestCaseHistories
            .GetPagedAsync(page, pageSize);

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