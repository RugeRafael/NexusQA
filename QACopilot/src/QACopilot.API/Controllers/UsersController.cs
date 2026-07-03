using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly QACopilotDbContext _context;

    public UsersController(QACopilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                TotalDocuments = _context.Documents.Count(d => d.UserId == u.Id),
                TotalTestCasesGenerated = _context.TestCaseHistories
                    .Where(t => t.UserId == u.Id)
                    .Sum(t => (int?)t.TotalTestCases) ?? 0
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return Ok(ApiResponse<List<UserDto>>.Ok(users));
    }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalTestCasesGenerated { get; set; }
}