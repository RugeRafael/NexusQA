namespace QACopilot.Domain.Entities;

public class ProjectAssignment
{
    public Guid Id { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AssignedByUserId { get; set; }
    public User AssignedByUser { get; set; } = null!;
}