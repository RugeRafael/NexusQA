namespace QACopilot.Application.DTOs.Reports;

public class ReportRequestDto
{
    public string ReportType { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string QAEngineer { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string Period { get; set; } = string.Empty;
    public string AdditionalContext { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public List<string> TestCases { get; set; } = new();
    public List<string> Defects { get; set; } = new();
    //public List<System.Text.Json.JsonElement> JiraBugs { get; set; } = new();
    public string? JiraBugs { get; set; }
public string JiraBugsRaw { get; set; } = "[]";

    public int TotalTestCases { get; set; }
    public int PassedTestCases { get; set; }
    public int FailedTestCases { get; set; }
    public int BlockedTestCases { get; set; }
    public double TotalExecutionTimeMinutes { get; set; }
    public byte[]? DocumentContent { get; set; }
    public string? DocumentFileName { get; set; }
    public string? DocumentContentType { get; set; }
    

}