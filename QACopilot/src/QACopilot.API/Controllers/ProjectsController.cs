using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.Projects;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectService projectService,
        ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "QAEngineer")]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto request)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _projectService.CreateAsync(request, userId);
        return Ok(ApiResponse<ProjectResponseDto>.Ok(result, "Project created successfully."));
    }

    [HttpGet]
    [Authorize(Policy = "QAEngineer")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _projectService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ProjectResponseDto>>.Ok(result));
    }

    [HttpGet("my-projects")]
    public async Task<IActionResult> GetMyProjects()
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _projectService.GetByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<ProjectResponseDto>>.Ok(result));
    }

    [HttpPost("assign")]
    [Authorize(Policy = "QAEngineer")]
    public async Task<IActionResult> AssignUser([FromBody] AssignProjectDto request)
    {
        var assignedBy = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _projectService.AssignUserAsync(request, assignedBy);
        return Ok(ApiResponse<ProjectResponseDto>.Ok(result, "User assigned to project successfully."));
    }

    [HttpDelete("{projectId}/users/{userId}")]
    [Authorize(Policy = "QAEngineer")]
    public async Task<IActionResult> UnassignUser(Guid projectId, Guid userId)
    {
        await _projectService.UnassignUserAsync(projectId, userId);
        return Ok(ApiResponse<object>.Ok(new { }, "User unassigned from project."));
    }
}