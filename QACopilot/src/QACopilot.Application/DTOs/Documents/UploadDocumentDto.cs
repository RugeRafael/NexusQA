namespace QACopilot.Application.DTOs.Documents;

public class UploadDocumentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public byte[] FileContent { get; set; } = [];
    public string Description { get; set; } = string.Empty;
}