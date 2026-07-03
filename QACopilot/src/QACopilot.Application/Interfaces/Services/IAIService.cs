using QACopilot.Application.DTOs.TestCases;

namespace QACopilot.Application.Interfaces.Services;

public interface IAIService
{
    Task<AIGenerationResultDto> GenerateTestCasesAsync(string documentContent);
}