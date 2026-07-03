namespace QACopilot.Application.DTOs.Projects;

public class AssignProjectDto
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string? Notes { get; set; }
}