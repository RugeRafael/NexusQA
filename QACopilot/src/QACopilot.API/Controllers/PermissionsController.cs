using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly QACopilotDbContext _context;

    public static readonly string[] SystemModules =
    [
        "dashboard", "my-projects", "documents", "testcases",
        "testplan", "history", "chat", "projects",
        "reports", "analytics", "senior-panel", "training", "metrics"
    ];

    private static readonly Dictionary<string, string[]> DefaultPermissions = new()
    {
        ["Administrador"]   = SystemModules,
        ["IngenieroSenior"] = ["dashboard","my-projects","documents","testcases","testplan","history","chat","projects","reports","analytics","senior-panel"],
        ["IngenieroQA"]     = ["dashboard","my-projects","documents","testcases","testplan","history","chat"],
        ["Scrum"]           = ["dashboard","my-projects","documents","reports","history"],
        ["Admin"]           = SystemModules,
        ["Senior"]          = ["dashboard","my-projects","documents","testcases","testplan","history","chat","projects","reports","analytics","senior-panel"],
        ["Junior"]          = ["dashboard","my-projects","documents","testcases","testplan","history","chat"],
    };

    public PermissionsController(QACopilotDbContext context)
    {
        _context = context;
    }

    [HttpGet("my-modules")]
    public async Task<IActionResult> GetMyModules()
    {
        var roleClaim = User.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
            c.Type == "role");
        var role = roleClaim?.Value ?? "IngenieroQA";

        var permissions = await _context.ModulePermissions
            .Where(p => p.Role == role && p.IsEnabled)
            .Select(p => p.ModuleKey)
            .ToListAsync();

        if (!permissions.Any())
        {
            permissions = DefaultPermissions.TryGetValue(role, out var defaults)
                ? [.. defaults]
                : ["dashboard"];
        }

        return Ok(ApiResponse<object>.Ok(new { role, modules = permissions }));
    }

    [HttpGet]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var roles = new[] { "Administrador", "IngenieroSenior", "IngenieroQA", "Scrum" };
        var result = new List<object>();

        foreach (var role in roles)
        {
            var saved = await _context.ModulePermissions
                .Where(p => p.Role == role)
                .ToListAsync();

            var modules = SystemModules.Select(m => new
            {
                moduleKey = m,
                isEnabled = saved.Any()
                    ? saved.Any(p => p.ModuleKey == m && p.IsEnabled)
                    : (DefaultPermissions.TryGetValue(role, out var def) && def.Contains(m))
            }).ToList();

            result.Add(new { role, modules });
        }

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("{role}")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> UpdateRolePermissions(string role, [FromBody] UpdatePermissionsRequest request)
    {
        var existing = await _context.ModulePermissions
            .Where(p => p.Role == role)
            .ToListAsync();
        _context.ModulePermissions.RemoveRange(existing);

        var newPermissions = request.Modules.Select(m => new ModulePermission
        {
            Id = Guid.NewGuid(),
            Role = role,
            ModuleKey = m.ModuleKey,
            IsEnabled = m.IsEnabled,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.ModulePermissions.AddRangeAsync(newPermissions);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, $"Permisos de {role} actualizados."));
    }
}

public class UpdatePermissionsRequest
{
    public List<ModulePermissionItem> Modules { get; set; } = [];
}

public class ModulePermissionItem
{
    public string ModuleKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
