using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Chat;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly QACopilotDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(QACopilotDbContext context, IAIService aiService, ILogger<ChatService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<ChatResponseDto> SendMessageAsync(ChatRequestDto request, Guid userId)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid();
        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
        {
            session = new ChatSession
            {
                Id = sessionId,
                UserId = userId,
                Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ChatSessions.Add(session);
        }

        var history = session.Messages?
            .OrderBy(m => m.CreatedAt)
            .TakeLast(10)
            .Select(m => new Dictionary<string, string> { ["role"] = m.Role, ["content"] = m.Content })
            .ToList() ?? new List<Dictionary<string, string>>();

        var projectId = request.ProjectId?.ToString() ?? "global";
        _logger.LogInformation("Chat for project {ProjectId}: {Msg}", projectId, request.Message[..Math.Min(80, request.Message.Length)]);

        var response = await _aiService.ChatAsync(request.Message, history, projectId);

        _context.ChatMessages.Add(new ChatMessage { Id = Guid.NewGuid(), SessionId = sessionId, Role = "user", Content = request.Message, CreatedAt = DateTime.UtcNow });
        var assistantMsg = new ChatMessage { Id = Guid.NewGuid(), SessionId = sessionId, Role = "assistant", Content = response, CreatedAt = DateTime.UtcNow };
        _context.ChatMessages.Add(assistantMsg);

        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ChatResponseDto { SessionId = sessionId, Message = response, CreatedAt = assistantMsg.CreatedAt };
    }

    public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId)
    {
        return await _context.ChatMessages
            .Where(m => m.SessionId == sessionId && m.Session.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto { Id = m.Id, Role = m.Role, Content = m.Content, CreatedAt = m.CreatedAt })
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatResponseDto>> GetSessionsAsync(Guid userId)
    {
        return await _context.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new ChatResponseDto { SessionId = s.Id, Message = s.Title ?? "Conversacion", CreatedAt = s.UpdatedAt })
            .ToListAsync();
    }
}
