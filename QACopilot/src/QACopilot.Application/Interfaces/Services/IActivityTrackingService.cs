using QACopilot.Application.DTOs.Analytics;

namespace QACopilot.Application.Interfaces.Services;

public interface IActivityTrackingService
{
    Task TrackActivityAsync(Guid userId, string module, string action, string ipAddress, string userAgent);
    Task<IEnumerable<UserActivityDto>> GetLiveActivityAsync();
    Task EndSessionAsync(Guid userId, string module);
}