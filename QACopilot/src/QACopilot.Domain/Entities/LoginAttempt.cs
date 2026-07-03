namespace QACopilot.Domain.Entities;

public class LoginAttempt
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public bool IsBlocked { get; set; } = false;
}