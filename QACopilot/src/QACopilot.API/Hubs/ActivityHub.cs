using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QACopilot.API.Hubs;

[Authorize]
public class ActivityHub : Hub
{
    public async Task ReportActivity(string module, string action)
    {
        var userId = Context.User?.FindFirst("uid")?.Value;
        var userName = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        await Clients.Group("role_Senior")
            .SendAsync("UserActivity", new
            {
                userId,
                userName,
                module,
                action,
                timestamp = DateTime.UtcNow
            });

        await Clients.Group("role_Admin")
            .SendAsync("UserActivity", new
            {
                userId,
                userName,
                module,
                action,
                timestamp = DateTime.UtcNow
            });
    }
}