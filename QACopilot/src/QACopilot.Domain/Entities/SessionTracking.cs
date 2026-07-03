namespace QACopilot.Domain.Entities;

public class SessionTracking
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public long DurationSeconds { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}