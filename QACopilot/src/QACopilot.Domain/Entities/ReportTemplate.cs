namespace QACopilot.Domain.Entities;

public class ReportTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StructureJson { get; set; } = string.Empty;
    public string AIInstructions { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}