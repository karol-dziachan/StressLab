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
    private readonly ITestResultHistoryService? _historyService;

    public ScenarioExecutionService(
        ILogger<ScenarioExecutionService> logger,
        IScenarioConfigurationService scenarioConfigurationService,
        IPerformanceTestService performanceTestService,
        ISystemMetricsService systemMetricsService,
        ITestResultHistoryService? historyService = null)
    {
        _logger = logger;
        _scenarioConfigurationService = scenarioConfigurationService;
        _performanceTestService = performanceTestService;
        _systemMetricsService = systemMetricsService;
        _historyService = historyService;
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
                ApiEndpoint = GetConfigurationValue(step.Configuration, "Url") ?? step.ApiEndpoint ?? string.Empty,
                ApiMethod = GetConfigurationValue(step.Configuration, "Method") ?? step.ApiMethod ?? string.Empty,
                SqlConnectionString = GetConfigurationValue(step.Configuration, "ConnectionString") ?? step.SqlConnectionString ?? string.Empty,
                SqlProcedureName = GetConfigurationValue(step.Configuration, "ProcedureName") ?? step.SqlProcedureName ?? string.Empty
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
                    ApiEndpoint = GetConfigurationValue(previousStep.Configuration, "Url") ?? previousStep.ApiEndpoint ?? string.Empty,
                    ApiMethod = GetConfigurationValue(previousStep.Configuration, "Method") ?? previousStep.ApiMethod ?? string.Empty,
                    SqlConnectionString = GetConfigurationValue(previousStep.Configuration, "ConnectionString") ?? previousStep.SqlConnectionString ?? string.Empty,
                    SqlProcedureName = GetConfigurationValue(previousStep.Configuration, "ProcedureName") ?? previousStep.SqlProcedureName ?? string.Empty
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
        
        // Log scenario result to history if history service is available
        if (_historyService is not null)
        {
            try
            {
                _logger.LogInformation("🔄 Logging scenario result to database: {ScenarioName}", scenario.Name);
                await _historyService.LogTestResultAsync(aggregatedResult, cancellationToken);
                _logger.LogInformation("✅ Scenario result successfully saved to database: {ScenarioName}", scenario.Name);
                
                // TODO: Check for performance deviations and send alerts
                // After logging to history, analyze deviations and send email alerts if needed
                // Example integration:
                // var analysis = await _historyService.AnalyzePerformanceDeviationAsync(aggregatedResult, cancellationToken: cancellationToken);
                // if (analysis is not null && (analysis.OverallDeviationScore > 20 || analysis.TrendDirection == TrendDirection.Degrading))
                // {
                //     await _emailNotificationService.SendPerformanceAlertAsync(aggregatedResult.TestName, analysis, cancellationToken);
                // }
                
                // ANALIZA ODCHYLENIA OD ŚREDNIEJ WYKONANIA POPRZEDNICH TESTÓW O TEJ SAMEJ NAZWIE
                try
                {
                    _logger.LogInformation("🔍 Analyzing deviation from average for scenario: {ScenarioName}", scenario.Name);
                    var averageDeviationAnalysis = await _historyService.AnalyzeDeviationFromAverageAsync(aggregatedResult, cancellationToken: cancellationToken);
                    
                    if (averageDeviationAnalysis != null)
                    {
                        _logger.LogInformation("📊 Average deviation analysis completed for {ScenarioName}:", scenario.Name);
                        _logger.LogInformation("   Overall Deviation: {OverallDeviation:F1}%", averageDeviationAnalysis.OverallDeviationScore);
                        _logger.LogInformation("   Response Time Deviation: {ResponseTimeDeviation:F1}%", averageDeviationAnalysis.ResponseTimeDeviationPercent);
                        _logger.LogInformation("   Error Rate Deviation: {ErrorRateDeviation:F1}%", averageDeviationAnalysis.ErrorRateDeviationPercent);
                        _logger.LogInformation("   Throughput Deviation: {ThroughputDeviation:F1}%", averageDeviationAnalysis.ThroughputDeviationPercent);
                        _logger.LogInformation("   CPU Usage Deviation: {CpuDeviation:F1}%", averageDeviationAnalysis.CpuUsageDeviationPercent);
                        _logger.LogInformation("   Memory Usage Deviation: {MemoryDeviation:F1}%", averageDeviationAnalysis.MemoryUsageDeviationPercent);
                        _logger.LogInformation("   Trend Direction: {TrendDirection}", averageDeviationAnalysis.TrendDirection);
                        _logger.LogInformation("   Sample Size: {SampleSize}", averageDeviationAnalysis.SampleSize);
                        
                        // Log historical averages
                        _logger.LogInformation("📈 Historical Averages:");
                        _logger.LogInformation("   Response Time: {ResponseTime:F1}ms", averageDeviationAnalysis.HistoricalAverageResponseTimeMs);
                        _logger.LogInformation("   Error Rate: {ErrorRate:F1}%", averageDeviationAnalysis.HistoricalAverageErrorRatePercent);
                        _logger.LogInformation("   Throughput: {Throughput:F1} req/s", averageDeviationAnalysis.HistoricalAverageRequestsPerSecond);
                        _logger.LogInformation("   CPU Usage: {CpuUsage:F1}%", averageDeviationAnalysis.HistoricalAverageCpuUsagePercent);
                        _logger.LogInformation("   Memory Usage: {MemoryUsage:F1}%", averageDeviationAnalysis.HistoricalAverageMemoryUsagePercent);
                        
                        // Log current metrics
                        _logger.LogInformation("📊 Current Metrics:");
                        _logger.LogInformation("   Response Time: {ResponseTime:F1}ms", averageDeviationAnalysis.CurrentAverageResponseTimeMs);
                        _logger.LogInformation("   Error Rate: {ErrorRate:F1}%", averageDeviationAnalysis.CurrentErrorRatePercent);
                        _logger.LogInformation("   Throughput: {Throughput:F1} req/s", averageDeviationAnalysis.CurrentRequestsPerSecond);
                        _logger.LogInformation("   CPU Usage: {CpuUsage:F1}%", averageDeviationAnalysis.CurrentCpuUsagePercent);
                        _logger.LogInformation("   Memory Usage: {MemoryUsage:F1}%", averageDeviationAnalysis.CurrentMemoryUsagePercent);
                        
                        // Check for significant deviations and generate TODO comments
                        if (averageDeviationAnalysis.OverallDeviationScore > 25)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - OGÓLNE ODCHYLENIE OD ŚREDNIEJ WYŻSZE NIŻ 25%
                            // Test: {scenario.Name}
                            // Ogólne odchylenie od średniej: {averageDeviationAnalysis.OverallDeviationScore:F1}%
                            // Średnia historyczna: {averageDeviationAnalysis.HistoricalAverageResponseTimeMs:F1}ms response time, {averageDeviationAnalysis.HistoricalAverageErrorRatePercent:F1}% error rate
                            // Aktualne: {averageDeviationAnalysis.CurrentAverageResponseTimeMs:F1}ms response time, {averageDeviationAnalysis.CurrentErrorRatePercent:F1}% error rate
                            // Data analizy: {averageDeviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendAverageDeviationAlertAsync(
                            //     scenario.Name,
                            //     averageDeviationAnalysis.OverallDeviationScore,
                            //     averageDeviationAnalysis.HistoricalAverageResponseTimeMs,
                            //     averageDeviationAnalysis.CurrentAverageResponseTimeMs,
                            //     averageDeviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("🚨 SIGNIFICANT DEVIATION FROM AVERAGE: {ScenarioName} - Overall deviation {Deviation:F1}% exceeds 25% threshold", 
                                scenario.Name, averageDeviationAnalysis.OverallDeviationScore);
                        }
                        
                        if (averageDeviationAnalysis.ResponseTimeDeviationPercent > 40)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - RESPONSE TIME ODCHYLENIE OD ŚREDNIEJ WYŻSZE NIŻ 40%
                            // Test: {scenario.Name}
                            // Response time odchylenie od średniej: {averageDeviationAnalysis.ResponseTimeDeviationPercent:F1}%
                            // Średnia historyczna: {averageDeviationAnalysis.HistoricalAverageResponseTimeMs:F1}ms
                            // Aktualne: {averageDeviationAnalysis.CurrentAverageResponseTimeMs:F1}ms
                            // Data: {averageDeviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendResponseTimeAverageDeviationAlertAsync(
                            //     scenario.Name,
                            //     averageDeviationAnalysis.ResponseTimeDeviationPercent,
                            //     averageDeviationAnalysis.HistoricalAverageResponseTimeMs,
                            //     averageDeviationAnalysis.CurrentAverageResponseTimeMs,
                            //     averageDeviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("⚠️ RESPONSE TIME DEVIATION FROM AVERAGE: {ScenarioName} - Response time deviation {Deviation:F1}% exceeds 40% threshold", 
                                scenario.Name, averageDeviationAnalysis.ResponseTimeDeviationPercent);
                        }
                        
                        if (averageDeviationAnalysis.ErrorRateDeviationPercent > 60)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - ERROR RATE ODCHYLENIE OD ŚREDNIEJ WYŻSZE NIŻ 60%
                            // Test: {scenario.Name}
                            // Error rate odchylenie od średniej: {averageDeviationAnalysis.ErrorRateDeviationPercent:F1}%
                            // Średnia historyczna: {averageDeviationAnalysis.HistoricalAverageErrorRatePercent:F1}%
                            // Aktualne: {averageDeviationAnalysis.CurrentErrorRatePercent:F1}%
                            // Data: {averageDeviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendErrorRateAverageDeviationAlertAsync(
                            //     scenario.Name,
                            //     averageDeviationAnalysis.ErrorRateDeviationPercent,
                            //     averageDeviationAnalysis.HistoricalAverageErrorRatePercent,
                            //     averageDeviationAnalysis.CurrentErrorRatePercent,
                            //     averageDeviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("🚨 ERROR RATE DEVIATION FROM AVERAGE: {ScenarioName} - Error rate deviation {Deviation:F1}% exceeds 60% threshold", 
                                scenario.Name, averageDeviationAnalysis.ErrorRateDeviationPercent);
                        }
                        
                        if (averageDeviationAnalysis.ThroughputDeviationPercent < -30)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - THROUGHPUT ODCHYLENIE OD ŚREDNIEJ NIŻSZE NIŻ -30%
                            // Test: {scenario.Name}
                            // Throughput odchylenie od średniej: {averageDeviationAnalysis.ThroughputDeviationPercent:F1}%
                            // Średnia historyczna: {averageDeviationAnalysis.HistoricalAverageRequestsPerSecond:F1} req/s
                            // Aktualne: {averageDeviationAnalysis.CurrentRequestsPerSecond:F1} req/s
                            // Data: {averageDeviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendThroughputAverageDeviationAlertAsync(
                            //     scenario.Name,
                            //     averageDeviationAnalysis.ThroughputDeviationPercent,
                            //     averageDeviationAnalysis.HistoricalAverageRequestsPerSecond,
                            //     averageDeviationAnalysis.CurrentRequestsPerSecond,
                            //     averageDeviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("⚠️ THROUGHPUT DEVIATION FROM AVERAGE: {ScenarioName} - Throughput deviation {Deviation:F1}% below -30% threshold", 
                                scenario.Name, averageDeviationAnalysis.ThroughputDeviationPercent);
                        }
                        
                        // Sprawdź czy są jakieś rekomendacje
                        if (averageDeviationAnalysis.Recommendations != null && averageDeviationAnalysis.Recommendations.Count > 0)
                        {
                            var recommendationsText = string.Join("; ", averageDeviationAnalysis.Recommendations);
                            _logger.LogInformation("💡 Recommendations for {ScenarioName}: {Recommendations}", 
                                scenario.Name, recommendationsText);
                            
                            // TODO: WYŚLIJ ALERT EMAIL Z REKOMENDACJAMI NA PODSTAWIE ŚREDNIEJ
                            // Test: {scenario.Name}
                            // Rekomendacje: {recommendationsText}
                            // Data: {averageDeviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendAverageBasedRecommendationsAlertAsync(
                            //     scenario.Name,
                            //     recommendationsText,
                            //     averageDeviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ No historical data available for average deviation analysis of {ScenarioName}", scenario.Name);
                    }
                }
                catch (Exception analysisEx)
                {
                    _logger.LogError(analysisEx, "❌ Failed to analyze deviation from average for scenario: {ScenarioName}", scenario.Name);
                    // Nie rzucamy wyjątku tutaj, bo analiza odchylenia nie powinna przerywać wykonania scenariusza
                }
                
                // ANALIZA ODCHYLENIA OD HISTORYCZNYCH LOGÓW W BAZIE DANYCH (STARA METODA)
                try
                {
                    _logger.LogInformation("🔍 Analyzing performance deviation for scenario: {ScenarioName}", scenario.Name);
                    var deviationAnalysis = await _historyService.AnalyzePerformanceDeviationAsync(aggregatedResult, cancellationToken: cancellationToken);
                    
                    if (deviationAnalysis != null)
                    {
                        _logger.LogInformation("📊 Deviation analysis completed for {ScenarioName}:", scenario.Name);
                        _logger.LogInformation("   Overall Deviation: {OverallDeviation:F1}%", deviationAnalysis.OverallDeviationScore);
                        _logger.LogInformation("   Response Time Deviation: {ResponseTimeDeviation:F1}%", deviationAnalysis.ResponseTimeDeviationPercent);
                        _logger.LogInformation("   Error Rate Deviation: {ErrorRateDeviation:F1}%", deviationAnalysis.ErrorRateDeviationPercent);
                        _logger.LogInformation("   Throughput Deviation: {ThroughputDeviation:F1}%", deviationAnalysis.ThroughputDeviationPercent);
                        _logger.LogInformation("   Trend Direction: {TrendDirection}", deviationAnalysis.TrendDirection);
                        
                        // TODO: Sprawdź czy odchylenie jest większe niż próg i wyślij alert
                        // Próg odchylenia: 20% dla ogólnego odchylenia, 30% dla response time, 50% dla error rate
                        if (deviationAnalysis.OverallDeviationScore > 20)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - OGÓLNE ODCHYLENIE WYŻSZE NIŻ 20%
                            // Test: {scenario.Name}
                            // Ogólne odchylenie: {deviationAnalysis.OverallDeviationScore:F1}%
                            // Data analizy: {deviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendCriticalPerformanceAlertAsync(
                            //     scenario.Name,
                            //     deviationAnalysis.OverallDeviationScore,
                            //     deviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("🚨 CRITICAL DEVIATION: {ScenarioName} - Overall deviation {Deviation:F1}% exceeds 20% threshold", 
                                scenario.Name, deviationAnalysis.OverallDeviationScore);
                        }
                        
                        if (deviationAnalysis.ResponseTimeDeviationPercent > 30)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - RESPONSE TIME DEGRADACJA WYŻSZA NIŻ 30%
                            // Test: {scenario.Name}
                            // Response time odchylenie: {deviationAnalysis.ResponseTimeDeviationPercent:F1}%
                            // Baseline: {deviationAnalysis.BaselineAverageResponseTimeMs:F1}ms
                            // Aktualne: {deviationAnalysis.CurrentAverageResponseTimeMs:F1}ms
                            // Data: {deviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendResponseTimeAlertAsync(
                            //     scenario.Name,
                            //     deviationAnalysis.ResponseTimeDeviationPercent,
                            //     deviationAnalysis.BaselineAverageResponseTimeMs,
                            //     deviationAnalysis.CurrentAverageResponseTimeMs,
                            //     deviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("⚠️ RESPONSE TIME DEGRADATION: {ScenarioName} - Response time deviation {Deviation:F1}% exceeds 30% threshold", 
                                scenario.Name, deviationAnalysis.ResponseTimeDeviationPercent);
                        }
                        
                        if (deviationAnalysis.ErrorRateDeviationPercent > 50)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - ERROR RATE SPIKE WYŻSZY NIŻ 50%
                            // Test: {scenario.Name}
                            // Error rate odchylenie: {deviationAnalysis.ErrorRateDeviationPercent:F1}%
                            // Baseline: {deviationAnalysis.BaselineErrorRatePercent:F1}%
                            // Aktualne: {deviationAnalysis.CurrentErrorRatePercent:F1}%
                            // Data: {deviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendErrorRateAlertAsync(
                            //     scenario.Name,
                            //     deviationAnalysis.ErrorRateDeviationPercent,
                            //     deviationAnalysis.BaselineErrorRatePercent,
                            //     deviationAnalysis.CurrentErrorRatePercent,
                            //     deviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("🚨 ERROR RATE SPIKE: {ScenarioName} - Error rate deviation {Deviation:F1}% exceeds 50% threshold", 
                                scenario.Name, deviationAnalysis.ErrorRateDeviationPercent);
                        }
                        
                        if (deviationAnalysis.ThroughputDeviationPercent < -20)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - THROUGHPUT DEGRADACJA NIŻSZA NIŻ -20%
                            // Test: {scenario.Name}
                            // Throughput odchylenie: {deviationAnalysis.ThroughputDeviationPercent:F1}%
                            // Baseline: {deviationAnalysis.BaselineRequestsPerSecond:F1} req/s
                            // Aktualne: {deviationAnalysis.CurrentRequestsPerSecond:F1} req/s
                            // Data: {deviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendThroughputAlertAsync(
                            //     scenario.Name,
                            //     deviationAnalysis.ThroughputDeviationPercent,
                            //     deviationAnalysis.BaselineRequestsPerSecond,
                            //     deviationAnalysis.CurrentRequestsPerSecond,
                            //     deviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("⚠️ THROUGHPUT DEGRADATION: {ScenarioName} - Throughput deviation {Deviation:F1}% below -20% threshold", 
                                scenario.Name, deviationAnalysis.ThroughputDeviationPercent);
                        }
                        
                        if (deviationAnalysis.TrendDirection == TrendDirection.Degrading)
                        {
                            // TODO: WYŚLIJ ALERT EMAIL - TREND DEGRADACJI
                            // Test: {scenario.Name}
                            // Trend: Degrading
                            // Ogólne odchylenie: {deviationAnalysis.OverallDeviationScore:F1}%
                            // Data: {deviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendTrendAlertAsync(
                            //     scenario.Name,
                            //     TrendDirection.Degrading,
                            //     deviationAnalysis.OverallDeviationScore,
                            //     deviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                            
                            _logger.LogWarning("📉 DEGRADING TREND: {ScenarioName} - Performance is degrading over time", scenario.Name);
                        }
                        
                        // Sprawdź czy są jakieś rekomendacje
                        if (deviationAnalysis.Recommendations != null && deviationAnalysis.Recommendations.Count > 0)
                        {
                            var recommendationsText = string.Join("; ", deviationAnalysis.Recommendations);
                            _logger.LogInformation("💡 Recommendations for {ScenarioName}: {Recommendations}", 
                                scenario.Name, recommendationsText);
                            
                            // TODO: WYŚLIJ ALERT EMAIL Z REKOMENDACJAMI
                            // Test: {scenario.Name}
                            // Rekomendacje: {recommendationsText}
                            // Data: {deviationAnalysis.AnalysisDate:yyyy-MM-dd HH:mm:ss}
                            // 
                            // Przykład integracji:
                            // await _emailNotificationService.SendRecommendationsAlertAsync(
                            //     scenario.Name,
                            //     recommendationsText,
                            //     deviationAnalysis.AnalysisDate,
                            //     cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ No historical data available for deviation analysis of {ScenarioName}", scenario.Name);
                    }
                }
                catch (Exception analysisEx)
                {
                    _logger.LogError(analysisEx, "❌ Failed to analyze performance deviation for scenario: {ScenarioName}", scenario.Name);
                    // Nie rzucamy wyjątku tutaj, bo analiza odchylenia nie powinna przerywać wykonania scenariusza
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"❌ CRITICAL ERROR: Failed to save scenario result to database for scenario: {scenario.Name}";
                _logger.LogError(ex, errorMsg);
                throw new InvalidOperationException(errorMsg, ex);
            }
        }
        else
        {
            _logger.LogWarning("⚠️ TestResultHistoryService is not registered - scenario results will not be saved to database");
        }
        
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
                TestConfigurationId = null, // Scenarios don't have a specific TestConfiguration
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
            TestConfigurationId = null, // Scenarios don't have a specific TestConfiguration
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