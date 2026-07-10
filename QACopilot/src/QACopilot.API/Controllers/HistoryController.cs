using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.TestCases;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly ITestCaseService _testCaseService;
    private readonly QACopilotDbContext _context;

    public HistoryController(ITestCaseService testCaseService, QACopilotDbContext context)
    {
        _testCaseService = testCaseService;
        _context = context;
    }

    // GET /api/history — solo del usuario actual
    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _testCaseService.GetHistoryAsync(page, pageSize, userId);
        return Ok(ApiResponse<PagedResultDto<TestCaseHistoryDto>>.Ok(result));
    }

    // GET /api/history/all — Admin/Senior ve todo por usuario
    [HttpGet("all")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> GetAllHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null)
    {
        var query = _context.TestCaseHistories
            .Include(t => t.Document)
            .Include(t => t.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id,
                documentName = t.Document != null ? t.Document.FileName : "",
                totalTestCases = t.TotalTestCases,
                confidenceScore = t.ConfidenceScore,
                status = t.Status,
                generatedAt = t.GeneratedAt,
                userName = t.User != null ? t.User.FullName : "Unknown",
                userEmail = t.User != null ? t.User.Email : ""
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }
}
