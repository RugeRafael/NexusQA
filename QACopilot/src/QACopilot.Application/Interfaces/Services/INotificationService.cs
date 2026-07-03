using QACopilot.Application.DTOs.Notifications;

namespace QACopilot.Application.Interfaces.Services;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, NotificationDto notification);
    Task NotifyProjectAssignmentAsync(Guid userId, ProjectAssignmentNotificationDto notification);
    Task BroadcastActivityAsync(ActivityNotificationDto notification);
    Task NotifyRoleGroupAsync(string role, NotificationDto notification);
}