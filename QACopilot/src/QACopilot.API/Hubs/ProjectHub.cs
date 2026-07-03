using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QACopilot.API.Hubs;

[Authorize]
public class ProjectHub : Hub
{
    public async Task NotifyProjectUpdate(string projectId, string message)
    {
        await Clients.Group($"project_{projectId}")
            .SendAsync("ProjectUpdated", new { projectId, message, timestamp = DateTime.UtcNow });
    }

    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
        await Clients.Caller.SendAsync("JoinedProject", projectId);
    }
}