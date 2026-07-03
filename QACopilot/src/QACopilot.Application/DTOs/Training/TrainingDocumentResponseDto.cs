namespace QACopilot.Application.DTOs.Training;

public class TrainingDocumentResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
}