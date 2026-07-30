using QACopilot.Application.DTOs.TestCases;

namespace QACopilot.Application.Interfaces.Services;

public interface IAIService
{
    Task<AIGenerationResultDto> GenerateTestCasesAsync(string documentContent, string? projectId = null);
    Task<string> ChatAsync(string message, List<Dictionary<string, string>>? sessionHistory = null, string? projectId = null, string? projectName = null);
}
