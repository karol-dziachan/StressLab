using Microsoft.Extensions.Logging;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Exceptions;
using StressLab.Core.Interfaces.Services;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for executing test scenarios
/// </summary>
public class ScenarioExecutionService : IScenarioExecutionService
{
    private readonly ILogger<ScenarioExecutionService> _logger;
    private readonly IScenarioConfigurationService _scenarioConfigurationService;
    private readonly IPerformanceTestService _performanceTestService;
    private readonly ISystemMetricsService _systemMetricsService;

    public ScenarioExecutionService(
        ILogger<ScenarioExecutionService> logger,
        IScenarioConfigurationService scenarioConfigurationService,
        IPerformanceTestService performanceTestService,
        ISystemMetricsService systemMetricsService)
    {
        _logger = logger;
        _scenarioConfigurationService = scenarioConfigurationService;
        _performanceTestService = performanceTestService;
        _systemMetricsService = systemMetricsService;
    }

    public async Task<TestResult> ExecuteScenarioAsync(TestScenario scenario, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution of scenario: {ScenarioName}", scenario.Name);

        await _systemMetricsService.StartMonitoringAsync(cancellationToken);

        var testResults = new List<TestResult>();
        TestResult currentResult = null!;

        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            var step = scenario.Steps[i];
            _logger.LogInformation("Executing step {StepName} ({StepType}) for scenario {ScenarioName}", step.Name, step.Type, scenario.Name);

            var testConfig = new TestConfiguration
            {
                Name = $"{scenario.Name} - {step.Name}",
                Description = $"Step {step.Name} of scenario {scenario.Name}",
                DurationSeconds = scenario.DurationSeconds,
                ConcurrentUsers = scenario.ConcurrentUsers,
                RampUpSeconds = scenario.RampUpSeconds,
                MaxErrorRatePercent = scenario.MaxErrorRatePercent,
                ExpectedResponseTimeMs = scenario.ExpectedResponseTimeMs,
                TestType = step.Type switch
                {
                    StepType.HttpApi => TestType.Api,
                    StepType.SqlProcedure => TestType.Sql,
                    StepType.SqlQuery => TestType.Sql,
                    _ => throw new DomainException($"Unsupported step type: {step.Type}")
                },
                ApiEndpoint = GetConfigurationValue(step.Configuration, "Url") ?? step.ApiEndpoint,
                ApiMethod = GetConfigurationValue(step.Configuration, "Method") ?? step.ApiMethod,
                SqlConnectionString = GetConfigurationValue(step.Configuration, "ConnectionString") ?? step.SqlConnectionString,
                SqlProcedureName = GetConfigurationValue(step.Configuration, "ProcedureName") ?? step.SqlProcedureName
            };

            if (step.IsCombinedWithPrevious && i > 0)
            {
                var previousStep = scenario.Steps[i - 1];
                _logger.LogInformation("Combining step {CurrentStepName} with previous step {PreviousStepName}", step.Name, previousStep.Name);
                
                // For combined tests, we need to pass both configurations.
                // This assumes the previous step was an API and current is SQL, or vice-versa.
                // The PerformanceTestService will handle the actual combination logic.
                var previousTestConfig = new TestConfiguration
                {
                    Name = $"{scenario.Name} - {previousStep.Name}",
                    Description = $"Step {previousStep.Name} of scenario {scenario.Name}",
                    DurationSeconds = scenario.DurationSeconds,
                    ConcurrentUsers = scenario.ConcurrentUsers,
                    RampUpSeconds = scenario.RampUpSeconds,
                    MaxErrorRatePercent = scenario.MaxErrorRatePercent,
                    ExpectedResponseTimeMs = scenario.ExpectedResponseTimeMs,
                    TestType = previousStep.Type switch
                {
                    StepType.HttpApi => TestType.Api,
                    StepType.SqlProcedure => TestType.Sql,
                    StepType.SqlQuery => TestType.Sql,
                    _ => throw new DomainException($"Unsupported step type: {previousStep.Type}")
                },
                    ApiEndpoint = GetConfigurationValue(previousStep.Configuration, "Url") ?? previousStep.ApiEndpoint,
                    ApiMethod = GetConfigurationValue(previousStep.Configuration, "Method") ?? previousStep.ApiMethod,
                    SqlConnectionString = GetConfigurationValue(previousStep.Configuration, "ConnectionString") ?? previousStep.SqlConnectionString,
                    SqlProcedureName = GetConfigurationValue(previousStep.Configuration, "ProcedureName") ?? previousStep.SqlProcedureName
                };
                
                currentResult = await _performanceTestService.ExecuteCombinedTestAsync(testConfig, cancellationToken);
            }
            else
            {
                currentResult = step.Type switch
                {
                    StepType.HttpApi => await _performanceTestService.ExecuteApiTestAsync(testConfig, cancellationToken),
                    StepType.SqlProcedure => await _performanceTestService.ExecuteSqlTestAsync(testConfig, cancellationToken),
                    StepType.SqlQuery => await _performanceTestService.ExecuteSqlTestAsync(testConfig, cancellationToken),
                    _ => throw new DomainException($"Unsupported test type for scenario step: {step.Type}")
                };
            }
            testResults.Add(currentResult);
        }

        await _systemMetricsService.StopMonitoringAsync();
        var systemMetrics = _systemMetricsService.GetMetrics();

        // Aggregate results from all steps into a single scenario result
        var aggregatedResult = AggregateScenarioResults(scenario, testResults, systemMetrics);

        _logger.LogInformation("Scenario {ScenarioName} completed with status: {Status}", scenario.Name, aggregatedResult.Status);
        return aggregatedResult;
    }

    public async Task<TestResult> ExecuteScenarioByNameAsync(string scenarioName, CancellationToken cancellationToken = default)
    {
        var scenario = _scenarioConfigurationService.GetScenario(scenarioName);
        if (scenario == null)
        {
            throw new DomainException($"Scenario '{scenarioName}' not found.");
        }
        return await ExecuteScenarioAsync(scenario, cancellationToken);
    }

    private TestResult AggregateScenarioResults(TestScenario scenario, List<TestResult> stepResults, SystemMetrics systemMetrics)
    {
        if (!stepResults.Any())
        {
            return new TestResult
            {
                Id = Guid.NewGuid(),
                TestConfigurationId = Guid.NewGuid(), // Temporary ID for scenario results
                TestName = scenario.Name,
                Description = scenario.Description,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow,
                Status = TestStatus.Failed,
                ErrorMessage = "No steps executed for scenario.",
                PerformanceImpact = PerformanceImpactLevel.None,
                MaxErrorRatePercent = scenario.MaxErrorRatePercent
            };
        }

        var totalRequests = stepResults.Sum(r => r.TotalRequests);
        var successfulRequests = stepResults.Sum(r => r.SuccessfulRequests);
        var failedRequests = stepResults.Sum(r => r.FailedRequests);
        var totalResponseTime = stepResults.Sum(r => r.AverageResponseTimeMs * r.TotalRequests); // Sum of (avg * count)
        var totalDuration = (stepResults.Max(r => r.EndTime) - stepResults.Min(r => r.StartTime))?.TotalSeconds ?? 0;

        var averageResponseTimeMs = totalRequests > 0 ? totalResponseTime / totalRequests : 0;
        var errorRatePercent = totalRequests > 0 ? (double)failedRequests / totalRequests * 100 : 0;
        var requestsPerSecond = totalDuration > 0 ? totalRequests / totalDuration : 0;

        var overallStatus = stepResults.All(r => r.Status == TestStatus.Completed && r.ErrorRatePercent <= r.MaxErrorRatePercent)
            ? TestStatus.Completed
            : TestStatus.Failed;

        var overallImpact = stepResults.Max(r => r.PerformanceImpact);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = Guid.NewGuid(), // Temporary ID for scenario results
            TestName = scenario.Name,
            Description = scenario.Description,
            StartTime = stepResults.Min(r => r.StartTime),
            EndTime = stepResults.Max(r => r.EndTime),
            Status = overallStatus,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            ErrorRatePercent = errorRatePercent,
            AverageResponseTimeMs = averageResponseTimeMs,
            P95ResponseTimeMs = stepResults.Max(r => r.P95ResponseTimeMs), // Simplistic aggregation, could be improved
            P99ResponseTimeMs = stepResults.Max(r => r.P99ResponseTimeMs), // Simplistic aggregation, could be improved
            RequestsPerSecond = requestsPerSecond,
            CpuUsagePercent = systemMetrics.CpuUsagePercent,
            MemoryUsagePercent = systemMetrics.MemoryUsagePercent,
            PerformanceImpact = overallImpact,
            MaxErrorRatePercent = scenario.MaxErrorRatePercent,
            ErrorMessage = overallStatus == TestStatus.Failed ? "One or more scenario steps failed." : null
        };
    }

    private static string? GetConfigurationValue(Dictionary<string, object> configuration, string key)
    {
        if (configuration.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }
}