using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Analytics;
using QACopilot.Application.DTOs.Notifications;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Services;

public class ActivityTrackingService : IActivityTrackingService
{
    private readonly QACopilotDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ActivityTrackingService> _logger;

    public ActivityTrackingService(
        QACopilotDbContext context,
        INotificationService notificationService,
        ILogger<ActivityTrackingService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task TrackActivityAsync(
        Guid userId, string module, string action,
        string ipAddress, string userAgent)
    {
        var tracking = new SessionTracking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Module = module,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            StartedAt = DateTime.UtcNow
        };

        await _context.SessionTrackings.AddAsync(tracking);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        if (user is null) return;

        await _notificationService.BroadcastActivityAsync(new ActivityNotificationDto
        {
            UserId = userId,
            UserName = user.FullName,
            Module = module,
            Action = action,
            OccurredAt = DateTime.UtcNow
        });
    }

    public async Task<IEnumerable<UserActivityDto>> GetLiveActivityAsync()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Include(u => u.Documents)
            .Include(u => u.TestCaseHistories)
            .ToListAsync();

        var activities = new List<UserActivityDto>();

        foreach (var user in users)
        {
            var sessions = await _context.SessionTrackings
                .Where(s => s.UserId == user.Id)
                .ToListAsync();

            var assignments = await _context.ProjectAssignments
                .Where(a => a.UserId == user.Id && a.IsActive)
                .Include(a => a.Project)
                .ToListAsync();

            var testPlans = await _context.TestPlanAnalyses
                .Where(t => t.UserId == user.Id)
                .CountAsync();

            activities.Add(new UserActivityDto
            {
                UserId = user.Id,
                UserName = user.FullName,
                Email = user.Email,
                TotalDocuments = user.Documents.Count,
                TotalTestCasesGenerated = user.TestCaseHistories.Count,
                TotalTestPlansAnalyzed = testPlans,
                TotalSessionSeconds = sessions.Sum(s => s.DurationSeconds),
                LastActivityAt = sessions.OrderByDescending(s => s.StartedAt)
                    .FirstOrDefault()?.StartedAt,
                AssignedProjects = assignments.Select(a => a.Project.Name)
            });
        }

        return activities;
    }

    public async Task EndSessionAsync(Guid userId, string module)
    {
        var session = await _context.SessionTrackings
            .Where(s => s.UserId == userId
                && s.Module == module
                && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        if (session is null) return;

        session.EndedAt = DateTime.UtcNow;
        session.DurationSeconds = (long)(session.EndedAt.Value - session.StartedAt).TotalSeconds;
        await _context.SaveChangesAsync();
    }
}