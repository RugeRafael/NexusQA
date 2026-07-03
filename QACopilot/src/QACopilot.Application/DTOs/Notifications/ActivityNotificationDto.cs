namespace QACopilot.Application.DTOs.Notifications;

public class ActivityNotificationDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}