using QACopilot.Application.DTOs.Analytics;

namespace QACopilot.Application.Interfaces.Services;

public interface ISessionTrackingService
{
    Task TrackAsync(Guid userId, string module, string action, long durationSeconds, string ipAddress, string userAgent);
    Task<IEnumerable<UserActivityDto>> GetAllUsersActivityAsync();
    Task<IEnumerable<SessionTrackingDto>> GetByUserAsync(Guid userId);
}