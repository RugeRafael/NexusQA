using QACopilot.Application.DTOs.TestCases;
namespace QACopilot.Application.Interfaces.Services;
public interface ITestCaseService
{
    Task<TestCaseResponseDto> GenerateAsync(GenerateTestCaseDto request, Guid userId);
    Task<PagedResultDto<TestCaseHistoryDto>> GetHistoryAsync(int page, int pageSize, Guid userId);
}
