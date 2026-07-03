namespace QACopilot.Domain.Entities;

public class SeniorPanelConfig
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public bool IndicatorsEnabled { get; set; } = false;
    public int MetaDocumentos { get; set; } = 3;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
