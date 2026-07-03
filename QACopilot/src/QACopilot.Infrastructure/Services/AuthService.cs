using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Auth;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly QACopilotDbContext _context;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        QACopilotDbContext context,
        TokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var ip = "system";
        const int maxAttempts = 5;
        const int lockMinutes = 15;

        var recentAttempts = await _context.LoginAttempts
            .Where(a => a.Email == request.Email
                && !a.Success
                && a.AttemptedAt >= DateTime.UtcNow.AddMinutes(-lockMinutes))
            .CountAsync();

        if (recentAttempts >= maxAttempts)
        {
            _logger.LogWarning("Account locked for {Email} after {Attempts} failed attempts",
                request.Email, recentAttempts);

            await _context.LoginAttempts.AddAsync(new LoginAttempt
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                IpAddress = ip,
                Success = false,
                FailureReason = "Account temporarily locked",
                IsBlocked = true,
                AttemptedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            throw new UnauthorizedAccessException(
                "Account temporarily locked. Try again in 15 minutes.");
        }

        if (!request.Email.EndsWith("@ithealth.co", StringComparison.OrdinalIgnoreCase))
        {
            await RegisterFailedAttemptAsync(request.Email, ip, "Invalid domain");
            throw new UnauthorizedAccessException("Access restricted to ithealth.co domain.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await RegisterFailedAttemptAsync(request.Email, ip, "Invalid credentials");
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        await _context.LoginAttempts.AddAsync(new LoginAttempt
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            IpAddress = ip,
            Success = true,
            AttemptedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return await GenerateTokensAsync(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (!request.Email.EndsWith("@ithealth.co", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Registration restricted to ithealth.co domain.");

        var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email}", user.Email);

        return await GenerateTokensAsync(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken
                && !r.IsRevoked
                && r.ExpiresAt > DateTime.UtcNow);

        if (refreshToken is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        return await GenerateTokensAsync(refreshToken.User);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} logged out", userId);
    }

    private async Task RegisterFailedAttemptAsync(string email, string ip, string reason)
    {
        await _context.LoginAttempts.AddAsync(new LoginAttempt
        {
            Id = Guid.NewGuid(),
            Email = email,
            IpAddress = ip,
            Success = false,
            FailureReason = reason,
            AttemptedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task<AuthResponseDto> GenerateTokensAsync(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserName = user.FullName,
            Role = user.Role
        };
    }
}