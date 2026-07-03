using QACopilot.Application.DTOs.TestPlan;

namespace QACopilot.Application.Interfaces.Services;

public interface ITestPlanService
{
    Task<TestPlanAnalysisResponseDto> UploadAndAnalyzeAsync(UploadTestPlanDto request, Guid userId);
    Task<IEnumerable<TestPlanAnalysisResponseDto>> GetByUserAsync(Guid userId);
    Task<TestPlanAnalysisResponseDto?> GetByIdAsync(Guid id);
}