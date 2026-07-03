using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QACopilot.Application.DTOs.Notifications;
using QACopilot.Application.DTOs.Projects;
using QACopilot.Application.Interfaces.Services;
using QACopilot.Domain.Entities;
using QACopilot.Domain.Enums;
using QACopilot.Domain.Exceptions;
using QACopilot.Infrastructure.Data.Context;

namespace QACopilot.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly QACopilotDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        QACopilotDbContext context,
        INotificationService notificationService,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ProjectResponseDto> CreateAsync(
        CreateProjectDto request, Guid createdByUserId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Status = ProjectStatus.Active.ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project {Name} created by user {UserId}",
            project.Name, createdByUserId);

        return await MapToDto(project);
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetAllAsync()
    {
        var projects = await _context.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.Assignments)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var dtos = new List<ProjectResponseDto>();
        foreach (var p in projects)
            dtos.Add(await MapToDto(p));

        return dtos;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetByUserAsync(Guid userId)
    {
        var assignments = await _context.ProjectAssignments
            .Where(a => a.UserId == userId && a.IsActive)
            .Include(a => a.Project)
                .ThenInclude(p => p.CreatedByUser)
            .Include(a => a.Project)
                .ThenInclude(p => p.Assignments)
            .ToListAsync();

        var dtos = new List<ProjectResponseDto>();
        foreach (var a in assignments)
            dtos.Add(await MapToDto(a.Project));

        return dtos;
    }

    public async Task<ProjectResponseDto> AssignUserAsync(
        AssignProjectDto request, Guid assignedByUserId)
    {
        var project = await _context.Projects
            .Include(p => p.CreatedByUser)
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists)
            throw new NotFoundException("User", request.UserId);

        var alreadyAssigned = await _context.ProjectAssignments
            .AnyAsync(a => a.ProjectId == request.ProjectId
                && a.UserId == request.UserId
                && a.IsActive);

        if (alreadyAssigned)
            throw new InvalidOperationException("User is already assigned to this project.");

        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            AssignedByUserId = assignedByUserId,
            Notes = request.Notes,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _context.ProjectAssignments.AddAsync(assignment);
        await _context.SaveChangesAsync();

        var assignedByUser = await _context.Users.FindAsync(assignedByUserId);

        await _notificationService.NotifyProjectAssignmentAsync(
            request.UserId,
            new ProjectAssignmentNotificationDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                AssignedByUserName = assignedByUser?.FullName ?? "Unknown",
                Notes = request.Notes,
                AssignedAt = DateTime.UtcNow
            });

        _logger.LogInformation("User {UserId} assigned to project {ProjectId} by {AssignedBy}",
            request.UserId, request.ProjectId, assignedByUserId);

        return await MapToDto(project);
    }

    public async Task UnassignUserAsync(Guid projectId, Guid userId)
    {
        var assignment = await _context.ProjectAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == projectId
                && a.UserId == userId
                && a.IsActive)
            ?? throw new NotFoundException("Assignment", $"{projectId}/{userId}");

        assignment.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task<ProjectResponseDto> MapToDto(Project project)
    {
        var assignmentCount = await _context.ProjectAssignments
            .CountAsync(a => a.ProjectId == project.Id && a.IsActive);

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreatedByUserName = project.CreatedByUser?.FullName ?? string.Empty,
            TotalAssignedQAs = assignmentCount,
            CreatedAt = project.CreatedAt
        };
    }
}