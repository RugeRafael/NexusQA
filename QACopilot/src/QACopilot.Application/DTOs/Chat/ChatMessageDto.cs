namespace QACopilot.Application.DTOs.Chat;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}