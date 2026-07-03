using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QACopilot.Infrastructure.Services;

namespace QACopilot.API.Hubs;

[Authorize]
public class AppNotificationHub : NotificationHub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("uid")?.Value;
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        if (role is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");

        await base.OnConnectedAsync();
    }
}