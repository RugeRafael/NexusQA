namespace QACopilot.Domain.Entities;

public class TestPlanAnalysis
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool? IsViable { get; set; }
    public string? ViabilityReason { get; set; }
    public string? EstimatedTimeJson { get; set; }
    public string? IstqbComplianceNotes { get; set; }
    public string? Iso29119ComplianceNotes { get; set; }
    public string? AIAnalysisResult { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnalyzedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? ReportHtml { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
}
