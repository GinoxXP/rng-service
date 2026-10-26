namespace RngService.PerformanceTests;

public class TestConfig
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string ReportFolder { get; set; } = "./reports";
    public List<ScenarioConfig> Scenarios { get; set; } = new();
}
