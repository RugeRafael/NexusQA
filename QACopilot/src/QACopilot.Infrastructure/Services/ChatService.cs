using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Chat;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Domain.Exceptions;
using QACopilot.Infrastructure.Data.Context;
using QACopilot.Infrastructure.Services.ExternalServices;

namespace QACopilot.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly QACopilotDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        QACopilotDbContext context,
        IAIService aiService,
        ILogger<ChatService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<ChatResponseDto> SendMessageAsync(ChatRequestDto request, Guid userId)
    {
        ChatSession session;

        if (request.SessionId.HasValue)
        {
            session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId)
                ?? throw new NotFoundException("ChatSession", request.SessionId);
        }
        else
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Message.Length > 50
                    ? request.Message[..50] + "..."
                    : request.Message,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            await _context.ChatSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        // Guardar mensaje del usuario
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "user",
            Content = request.Message,
            SentAt = DateTime.UtcNow
        };
        await _context.ChatMessages.AddAsync(userMessage);

        // Construir historial de la sesión para contexto
        var sessionHistory = new List<Dictionary<string, string>>();
        if (request.SessionId.HasValue)
        {
            var history = await _context.ChatMessages
                .Where(m => m.SessionId == session.Id && m.Id != userMessage.Id)
                .OrderBy(m => m.SentAt)
                .TakeLast(10)
                .ToListAsync();

            sessionHistory = history.Select(m => new Dictionary<string, string>
            {
                { "role", m.Role },
                { "content", m.Content }
            }).ToList();
        }

        // Llamar al endpoint /api/chat del microservicio Python
        string aiResponse;
        if (_aiService is AIService aiServiceConcrete)
        {
            aiResponse = await aiServiceConcrete.ChatAsync(request.Message, sessionHistory);
        }
        else
        {
            // Fallback usando GenerateTestCasesAsync
            var result = await _aiService.GenerateTestCasesAsync(
                $"[CHAT_QA_ASSISTANT]\n{request.Message}");
            aiResponse = result.Content;
        }

        // Guardar respuesta del asistente
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = "assistant",
            Content = aiResponse,
            SentAt = DateTime.UtcNow,
            TokensUsed = 0
        };
        await _context.ChatMessages.AddAsync(assistantMessage);

        session.LastMessageAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Chat message processed for session {SessionId}", session.Id);

        var fullHistory = await _context.ChatMessages
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                SentAt = m.SentAt
            })
            .ToListAsync();

        return new ChatResponseDto
        {
            SessionId = session.Id,
            Response = aiResponse,
            History = fullHistory
        };
    }

    public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
            ?? throw new NotFoundException("ChatSession", sessionId);

        return await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                SentAt = m.SentAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatResponseDto>> GetSessionsAsync(Guid userId)
    {
        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastMessageAt)
            .ToListAsync();

        return sessions.Select(s => new ChatResponseDto
        {
            SessionId = s.Id,
            Response = string.Empty,
            History = []
        });
    }
}
