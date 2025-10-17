using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Interfaces.Services;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for parsing and managing test scenarios from JSON configuration
/// </summary>
public class ScenarioConfigurationService : IScenarioConfigurationService
{
    private readonly ILogger<ScenarioConfigurationService> _logger;
    private readonly ScenarioConfigurationOptions _options;
    private readonly Dictionary<string, TestScenario> _scenarios = new();

    public ScenarioConfigurationService(
        ILogger<ScenarioConfigurationService> logger,
        IOptions<ScenarioConfigurationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task LoadScenariosAsync(string scenariosFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading scenarios from file: {FilePath}", scenariosFilePath);

        try
        {
            if (!File.Exists(scenariosFilePath))
            {
                throw new FileNotFoundException($"Scenarios file not found: {scenariosFilePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(scenariosFilePath, cancellationToken);
            var scenariosConfig = JsonSerializer.Deserialize<ScenariosConfiguration>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (scenariosConfig?.TestScenarios is null)
            {
                throw new InvalidOperationException("Invalid scenarios configuration format");
            }

            _scenarios.Clear();

            foreach (var scenarioConfig in scenariosConfig.TestScenarios)
            {
                var scenario = ParseScenario(scenarioConfig);
                _scenarios[scenario.Name] = scenario;
                _logger.LogDebug("Loaded scenario: {ScenarioName}", scenario.Name);
            }

            _logger.LogInformation("Successfully loaded {Count} scenarios", _scenarios.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scenarios from file: {FilePath}", scenariosFilePath);
            throw;
        }
    }

    public TestScenario? GetScenario(string scenarioName)
    {
        _scenarios.TryGetValue(scenarioName, out var scenario);
        return scenario;
    }

    public IEnumerable<TestScenario> GetAllScenarios()
    {
        return _scenarios.Values;
    }

    public IEnumerable<string> GetScenarioNames()
    {
        return _scenarios.Keys;
    }

    private TestScenario ParseScenario(ScenarioConfiguration scenarioConfig)
    {
        var steps = scenarioConfig.Steps?.Select(ParseStep).ToList() ?? new List<TestStep>();
        var loadSimulation = ParseLoadSimulation(scenarioConfig.LoadSimulation);

        return new TestScenario
        {
            Name = scenarioConfig.Name,
            Description = scenarioConfig.Description,
            Steps = steps,
            ExecutionMode = ParseExecutionMode(scenarioConfig.ExecutionMode),
            LoadSimulation = loadSimulation,
            Settings = scenarioConfig.Settings
        };
    }

    private TestStep ParseStep(StepConfiguration stepConfig)
    {
        return new TestStep
        {
            Name = stepConfig.Name,
            Description = stepConfig.Description,
            Type = ParseStepType(stepConfig.Type),
            Configuration = stepConfig.Configuration ?? new Dictionary<string, object>(),
            Weight = stepConfig.Weight,
            Enabled = stepConfig.Enabled,
            Settings = stepConfig.Settings
        };
    }

    private LoadSimulation ParseLoadSimulation(LoadSimulationConfiguration loadConfig)
    {
        return new LoadSimulation
        {
            Type = ParseLoadSimulationType(loadConfig.Type),
            Rate = loadConfig.Rate,
            DurationSeconds = loadConfig.DurationSeconds,
            RampUpSeconds = loadConfig.RampUpSeconds,
            MaxConcurrentUsers = loadConfig.MaxConcurrentUsers,
            Parameters = loadConfig.Parameters
        };
    }

    private StepType ParseStepType(string stepType)
    {
        return stepType.ToLowerInvariant() switch
        {
            "httpapi" or "http" or "api" => StepType.HttpApi,
            "sqlprocedure" or "sqlproc" or "procedure" => StepType.SqlProcedure,
            "sqlquery" or "sql" or "query" => StepType.SqlQuery,
            "wait" or "delay" => StepType.Wait,
            "customscript" or "script" => StepType.CustomScript,
            "fileoperation" or "file" => StepType.FileOperation,
            "databaseconnection" or "dbconnection" => StepType.DatabaseConnection,
            _ => throw new ArgumentException($"Unknown step type: {stepType}")
        };
    }

    private StepExecutionMode ParseExecutionMode(string executionMode)
    {
        return executionMode.ToLowerInvariant() switch
        {
            "parallel" => StepExecutionMode.Parallel,
            "sequential" => StepExecutionMode.Sequential,
            "grouped" => StepExecutionMode.Grouped,
            "weighted" => StepExecutionMode.Weighted,
            _ => StepExecutionMode.Parallel
        };
    }

    private LoadSimulationType ParseLoadSimulationType(string simulationType)
    {
        return simulationType.ToLowerInvariant() switch
        {
            "constantrate" or "constant" => LoadSimulationType.ConstantRate,
            "rampup" or "ramp" => LoadSimulationType.RampUp,
            "spike" => LoadSimulationType.Spike,
            "stress" => LoadSimulationType.Stress,
            "soak" => LoadSimulationType.Soak,
            _ => LoadSimulationType.ConstantRate
        };
    }
}

/// <summary>
/// Configuration options for scenario service
/// </summary>
public class ScenarioConfigurationOptions
{
    public string ScenariosFilePath { get; set; } = "scenarios.json";
    public bool AutoReload { get; set; } = false;
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// JSON configuration classes for deserialization
/// </summary>
public class ScenariosConfiguration
{
    public List<ScenarioConfiguration> TestScenarios { get; set; } = new();
    public GlobalSettings? GlobalSettings { get; set; }
}

public class ScenarioConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ExecutionMode { get; set; } = "Parallel";
    public LoadSimulationConfiguration LoadSimulation { get; set; } = new();
    public List<StepConfiguration> Steps { get; set; } = new();
    public Dictionary<string, object>? Settings { get; set; }
}

public class StepConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object>? Configuration { get; set; }
    public int Weight { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object>? Settings { get; set; }
}

public class LoadSimulationConfiguration
{
    public string Type { get; set; } = "ConstantRate";
    public int Rate { get; set; } = 10;
    public int DurationSeconds { get; set; } = 60;
    public int RampUpSeconds { get; set; } = 10;
    public int MaxConcurrentUsers { get; set; } = 100;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class GlobalSettings
{
    public int DefaultTimeout { get; set; } = 30000;
    public int DefaultRetryCount { get; set; } = 3;
    public int DefaultRetryDelay { get; set; } = 1000;
    public int MetricsCollectionInterval { get; set; } = 1000;
    public ReportGenerationSettings? ReportGeneration { get; set; }
    public LoggingSettings? Logging { get; set; }
    public PerformanceThresholds? PerformanceThresholds { get; set; }
}

public class ReportGenerationSettings
{
    public bool Html { get; set; } = true;
    public bool Json { get; set; } = true;
    public bool Csv { get; set; } = true;
}

public class LoggingSettings
{
    public string Level { get; set; } = "Information";
    public bool IncludeRequestDetails { get; set; } = true;
    public bool IncludeResponseDetails { get; set; } = false;
}

public class PerformanceThresholds
{
    public int MaxResponseTimeMs { get; set; } = 2000;
    public double MaxErrorRatePercent { get; set; } = 5.0;
    public int MinThroughputPerSecond { get; set; } = 10;
}

