using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QACopilot.API.Helpers;
using QACopilot.Infrastructure.Data.Context;
using BCrypt.Net;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "SeniorOrAdmin")]
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

    [HttpPost]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<object>.Fail("Email y contraseña son requeridos."));

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(ApiResponse<object>.Fail("Ya existe un usuario con ese email."));

        var user = new QACopilot.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role ?? "IngenieroQA",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { id = user.Id, email = user.Email }, "Usuario creado exitosamente."));
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        var validRoles = new[] { "Administrador", "IngenieroSenior", "IngenieroQA", "Scrum" };
        if (!validRoles.Contains(request.Role))
            return BadRequest(ApiResponse<object>.Fail("Rol inválido. Use: Admin, Senior, Junior."));

        user.Role = request.Role;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { id, role = request.Role }, "Rol actualizado."));
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        var msg = user.IsActive ? "Usuario activado." : "Usuario desactivado.";
        return Ok(ApiResponse<object>.Ok(new { id, isActive = user.IsActive }, msg));
    }

    [HttpPut("{id}/reset-password")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("La contraseña debe tener al menos 6 caracteres."));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Contraseña restablecida exitosamente."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SeniorOrAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));

        // Soft delete
        user.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null, "Usuario eliminado."));
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

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
