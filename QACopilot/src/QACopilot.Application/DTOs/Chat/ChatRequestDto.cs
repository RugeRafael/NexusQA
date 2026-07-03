namespace QACopilot.Application.DTOs.Chat;

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }
}