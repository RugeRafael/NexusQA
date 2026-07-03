using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Chat;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] ChatRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _chatService.SendMessageAsync(request, userId);
        return Ok(ApiResponse<ChatResponseDto>.Ok(result));
    }

    [HttpGet("sessions/{sessionId}/history")]
    public async Task<IActionResult> GetHistory(Guid sessionId)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _chatService.GetHistoryAsync(sessionId, userId);
        return Ok(ApiResponse<IEnumerable<ChatMessageDto>>.Ok(result));
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _chatService.GetSessionsAsync(userId);
        return Ok(ApiResponse<IEnumerable<ChatResponseDto>>.Ok(result));
    }
}