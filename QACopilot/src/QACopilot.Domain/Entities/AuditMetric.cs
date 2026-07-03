namespace QACopilot.Domain.Entities;

public class AuditMetric
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}