using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Notifications;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.Infrastructure.Services;

public class NotificationHub : Hub { }

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyUserAsync(Guid userId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);

        _logger.LogInformation("Notification sent to user {UserId}: {Title}",
            userId, notification.Title);
    }

    public async Task NotifyProjectAssignmentAsync(
        Guid userId, ProjectAssignmentNotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ProjectAssigned", notification);

        _logger.LogInformation("Project assignment notification sent to user {UserId}: {ProjectName}",
            userId, notification.ProjectName);
    }

    public async Task BroadcastActivityAsync(ActivityNotificationDto notification)
    {
        await _hubContext.Clients
            .Group("role_Senior")
            .SendAsync("TeamActivity", notification);

        await _hubContext.Clients
            .Group("role_Admin")
            .SendAsync("TeamActivity", notification);
    }

    public async Task NotifyRoleGroupAsync(string role, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"role_{role}")
            .SendAsync("ReceiveNotification", notification);
    }
}