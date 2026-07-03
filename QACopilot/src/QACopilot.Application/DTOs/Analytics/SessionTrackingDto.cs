namespace QACopilot.Application.DTOs.Analytics;

public class SessionTrackingDto
{
    public string Module { get; set; } = string.Empty;
    public long TotalSeconds { get; set; }
    public int TotalSessions { get; set; }
    public DateTime LastAccessAt { get; set; }
}