using QACopilot.Application.DTOs.Documents;

namespace QACopilot.Application.Interfaces.Services;

public interface IDocumentService
{
    Task<DocumentResponseDto> UploadAsync(UploadDocumentDto request, Guid userId);
    Task<IEnumerable<DocumentResponseDto>> GetByUserAsync(Guid userId);
}