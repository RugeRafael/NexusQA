using QACopilot.Application.DTOs.Reports;

namespace QACopilot.Application.Interfaces.Services;

public interface IReportService
{
    Task<ReportResponseDto> GenerateComparisonReportAsync(ReportRequestDto request);
    Task<ReportResponseDto> GenerateCompletionReportAsync(ReportRequestDto request);
    Task<ReportResponseDto> GenerateInnovationReportAsync(ReportRequestDto request);
}