using QACopilot.Application.DTOs.Documents;
using QACopilot.Application.DTOs.Training;

namespace QACopilot.Application.Interfaces.Services;

public interface ITrainingDocumentService
{
    Task<TrainingDocumentResponseDto> UploadAsync(UploadDocumentDto request, string category, Guid userId);
    Task<IEnumerable<TrainingDocumentResponseDto>> GetAllAsync();
    Task SetActiveAsync(Guid id, bool isActive);
}