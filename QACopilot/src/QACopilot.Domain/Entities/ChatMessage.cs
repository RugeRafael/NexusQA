namespace QACopilot.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Guid SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;
}