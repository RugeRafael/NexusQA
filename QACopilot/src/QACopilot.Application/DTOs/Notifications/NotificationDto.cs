namespace QACopilot.Application.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}