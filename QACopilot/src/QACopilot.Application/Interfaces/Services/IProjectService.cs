using QACopilot.Application.DTOs.Projects;

namespace QACopilot.Application.Interfaces.Services;

public interface IProjectService
{
    Task<ProjectResponseDto> CreateAsync(CreateProjectDto request, Guid createdByUserId);
    Task<IEnumerable<ProjectResponseDto>> GetAllAsync();
    Task<IEnumerable<ProjectResponseDto>> GetByUserAsync(Guid userId);
    Task<ProjectResponseDto> AssignUserAsync(AssignProjectDto request, Guid assignedByUserId);
    Task UnassignUserAsync(Guid projectId, Guid userId);
}