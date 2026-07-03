namespace QACopilot.Application.DTOs.Chat;

public class ChatResponseDto
{
    public Guid SessionId { get; set; }
    public string Response { get; set; } = string.Empty;
    public IEnumerable<ChatMessageDto> History { get; set; } = [];
}