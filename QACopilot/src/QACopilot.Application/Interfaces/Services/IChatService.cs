using QACopilot.Application.DTOs.Chat;

namespace QACopilot.Application.Interfaces.Services;

public interface IChatService
{
    Task<ChatResponseDto> SendMessageAsync(ChatRequestDto request, Guid userId);
    Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId);
    Task<IEnumerable<ChatResponseDto>> GetSessionsAsync(Guid userId);
}