namespace QACopilot.Application.DTOs.Notifications;

public class ProjectAssignmentNotificationDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string AssignedByUserName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}