namespace QACopilot.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<ChatMessage> Messages { get; set; } = [];
}