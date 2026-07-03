namespace QACopilot.Application.DTOs.TestPlan;

public class TestPlanAnalysisResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool? IsViable { get; set; }
    public string? ViabilityReason { get; set; }
    public string? EstimatedTimeJson { get; set; }
    public string? IstqbComplianceNotes { get; set; }
    public string? Iso29119ComplianceNotes { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? AnalyzedAt { get; set; }
}