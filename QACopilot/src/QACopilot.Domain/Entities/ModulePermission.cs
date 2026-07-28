namespace QACopilot.Domain.Entities;

public class ModulePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
