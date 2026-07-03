namespace QACopilot.Domain.Entities;

public class JiraConfig
{
    public Guid Id { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string AccountEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}