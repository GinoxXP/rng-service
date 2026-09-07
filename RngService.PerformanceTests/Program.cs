using System.Text.Json;
using CommonResources.PerformanceTests;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using RngService.PerformanceTests.Scenarios;

var configPath = Path.Combine(AppContext.BaseDirectory, "tests.json");
if (!File.Exists(configPath))
    throw new FileNotFoundException($"Configuration file {configPath} not founded!");

var jsonContent = File.ReadAllText(configPath);
var config = JsonSerializer.Deserialize<TestConfig>(jsonContent, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

Console.WriteLine($"Running load test for RngService by URL: {config.ServiceUrl}");

var availableScenarios = new Dictionary<string, Func<ScenarioProps>>(StringComparer.OrdinalIgnoreCase)
{
    ["PrepareRoundTest.Realistic"] = () => PrepareRoundTest.Realistic(config.ServiceUrl),
    ["RollDiceTest.Realistic"] = () => RollDiceTest.Realistic(config.ServiceUrl),
};

var activeScenarioNames = config.Scenarios
    .Where(s => s.Enabled)
    .Select(s => s.Name)
    .ToList();

var scenariosToRun = activeScenarioNames
    .Where(name => availableScenarios.ContainsKey(name))
    .Select(name => availableScenarios[name]())
    .ToArray();

if (scenariosToRun.Length == 0)
{
    Console.WriteLine("No active scenario founded (Enabled: true)");
    return;
}

var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var reportName = $"rng_service_{string.Join("_", activeScenarioNames).ToLower()}_{timestamp}";

NBomberRunner
    .RegisterScenarios(scenariosToRun)
    .WithReportFileName(reportName)
    .WithReportFolder(config.ReportFolder)
    .WithReportFormats(ReportFormat.Html)
    .Run();
