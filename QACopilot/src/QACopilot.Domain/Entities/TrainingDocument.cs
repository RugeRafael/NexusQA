namespace QACopilot.Domain.Entities;

public class TrainingDocument
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
}