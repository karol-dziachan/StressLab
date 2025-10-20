using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StressLab.Application.Commands;
using StressLab.Application.DTOs;
using StressLab.Core.Enums;
using StressLab.Core.Entities;
using StressLab.Core.Interfaces.Services;
using StressLab.Infrastructure.Configuration;
using StressLab.Infrastructure.Services;

namespace StressLab.PerformanceTests;

/// <summary>
/// Main program for executing performance tests
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/stresslab-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Starting StressLab Performance Tests");

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Build host
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddStressLabInfrastructure();
                    services.AddStressLabApplication();
                    services.AddStressLabLogging();
                })
                .UseSerilog()
                .Build();

            // Get services
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var scenarioService = host.Services.GetRequiredService<IScenarioConfigurationService>();
            var scenarioExecutionService = host.Services.GetRequiredService<IScenarioExecutionService>();
            var reportService = host.Services.GetRequiredService<IReportService>();
            var performanceTestService = host.Services.GetRequiredService<IPerformanceTestService>();

            // Parse command line arguments
            var testConfig = ParseCommandLineArguments(args, configuration);

            // Load scenarios from JSON
            var scenariosPath = Path.Combine(Directory.GetCurrentDirectory(), "scenarios.json");
            if (File.Exists(scenariosPath))
            {
                await scenarioService.LoadScenariosAsync(scenariosPath);
                logger.LogInformation("Loaded scenarios from: {ScenariosPath}", scenariosPath);
            }
            else
            {
                logger.LogWarning("Scenarios file not found: {ScenariosPath}", scenariosPath);
            }

            // Configure HttpClient if configuration is available
            var httpClientConfig = scenarioService.GetHttpClientConfiguration();
            if (httpClientConfig != null)
            {
                logger.LogInformation("Configuring HttpClient with custom settings");
                performanceTestService.ConfigureHttpClient(httpClientConfig);
            }

            // Execute tests based on configuration
            var allResults = new List<TestResult>();
            
            if (!string.IsNullOrEmpty(testConfig.ScenarioName))
            {
                // Execute single scenario by name
                logger.LogInformation("Executing single scenario: {ScenarioName}", testConfig.ScenarioName);
                var result = await scenarioExecutionService.ExecuteScenarioByNameAsync(testConfig.ScenarioName);
                allResults.Add(result);
            }
            else
            {
                // Execute all scenarios from JSON
                logger.LogInformation("Executing all scenarios from scenarios.json");
                
                var scenarios = scenarioService.GetAllScenarios();
                logger.LogInformation("Found {ScenarioCount} scenarios to execute", scenarios.Count());
                
                foreach (var scenario in scenarios)
                {
                    try
                    {
                        logger.LogInformation("Executing scenario: {ScenarioName}", scenario.Name);
                        var result = await scenarioExecutionService.ExecuteScenarioByNameAsync(scenario.Name);
                        allResults.Add(result);
                        
                        logger.LogInformation("Scenario {ScenarioName} completed successfully. Result ID: {ResultId}", 
                            scenario.Name, result.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to execute scenario: {ScenarioName}", scenario.Name);
                        
                        // Create a failed result for this scenario
                        var failedResult = new TestResult
                        {
                            Id = Guid.NewGuid(),
                            TestConfigurationId = Guid.NewGuid(),
                            TestName = scenario.Name,
                            Description = $"Failed scenario: {scenario.Name}",
                            Status = Core.Enums.TestStatus.Failed,
                            StartTime = DateTimeOffset.UtcNow,
                            EndTime = DateTimeOffset.UtcNow,
                            TotalRequests = 0,
                            SuccessfulRequests = 0,
                            FailedRequests = 0,
                            ErrorRatePercent = 100.0,
                            AverageResponseTimeMs = 0,
                            MinResponseTimeMs = 0,
                            MaxResponseTimeMs = 0,
                            P95ResponseTimeMs = 0,
                            P99ResponseTimeMs = 0,
                            RequestsPerSecond = 0,
                            CpuUsagePercent = 0,
                            MemoryUsagePercent = 0,
                            PerformanceImpact = Core.Enums.PerformanceImpactLevel.Critical,
                            ErrorMessage = ex.Message,
                            MaxErrorRatePercent = 5.0,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        allResults.Add(failedResult);
                    }
                }
            }

            logger.LogInformation("All tests completed. Total results: {ResultCount}", allResults.Count);

            // Generate reports for all results
            var reportsPath = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            Directory.CreateDirectory(reportsPath);

            var reportPaths = new List<(string Type, string Path)>();
            
            // Generate individual reports for each test
            foreach (var result in allResults)
            {
                try
                {
                    var htmlReportPath = await reportService.GenerateHtmlReportAsync(result, reportsPath);
                    var jsonReportPath = await reportService.GenerateJsonReportAsync(result, reportsPath);
                    var csvReportPath = await reportService.GenerateCsvReportAsync(result, reportsPath);
                    
                    reportPaths.Add(("HTML", htmlReportPath));
                    reportPaths.Add(("JSON", jsonReportPath));
                    reportPaths.Add(("CSV", csvReportPath));
                    
                    logger.LogInformation("Reports generated for {TestName}:", result.TestName);
                    logger.LogInformation("  HTML: {HtmlReport}", htmlReportPath);
                    logger.LogInformation("  JSON: {JsonReport}", jsonReportPath);
                    logger.LogInformation("  CSV: {CsvReport}", csvReportPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to generate reports for test: {TestName}", result.TestName);
                }
            }

            // Generate combined HTML report
            try
            {
                var combinedHtmlReportPath = await reportService.GenerateCombinedHtmlReportAsync(allResults, reportsPath);
                reportPaths.Add(("Combined HTML", combinedHtmlReportPath));
                logger.LogInformation("Combined HTML report generated: {CombinedReport}", combinedHtmlReportPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate combined HTML report");
            }

    // Output results for TeamCity (individual tests first)
    OutputTeamCityResults(allResults);

            Log.Information("StressLab Performance Tests completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "StressLab Performance Tests failed");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static TestConfigurationDto ParseCommandLineArguments(string[] args, IConfiguration configuration)
    {
        // Default configuration - no scenario by default to run all scenarios
        var testConfig = new TestConfigurationDto
        {
            Name = "All Scenarios Test",
            Description = "Execute all scenarios from scenarios.json",
            TestType = TestType.Api,
            DurationSeconds = 60,
            ConcurrentUsers = 10,
            RampUpSeconds = 10,
            ApiEndpoint = "https://httpbin.org/get",
            ApiMethod = "GET",
            ExpectedResponseTimeMs = 1000,
            MaxErrorRatePercent = 5.0,
            ScenarioName = null  // null means run all scenarios
        };

        // Override with configuration values
        testConfig = testConfig with
        {
            Name = configuration["TestConfiguration:Name"] ?? testConfig.Name,
            Description = configuration["TestConfiguration:Description"] ?? testConfig.Description,
            TestType = Enum.TryParse<TestType>(configuration["TestConfiguration:TestType"], out var testType) ? testType : testConfig.TestType,
            DurationSeconds = int.TryParse(configuration["TestConfiguration:DurationSeconds"], out var duration) ? duration : testConfig.DurationSeconds,
            ConcurrentUsers = int.TryParse(configuration["TestConfiguration:ConcurrentUsers"], out var users) ? users : testConfig.ConcurrentUsers,
            RampUpSeconds = int.TryParse(configuration["TestConfiguration:RampUpSeconds"], out var rampUp) ? rampUp : testConfig.RampUpSeconds,
            ApiEndpoint = configuration["TestConfiguration:ApiEndpoint"] ?? testConfig.ApiEndpoint,
            ApiMethod = configuration["TestConfiguration:ApiMethod"] ?? testConfig.ApiMethod,
            SqlConnectionString = configuration["TestConfiguration:SqlConnectionString"],
            SqlProcedureName = configuration["TestConfiguration:SqlProcedureName"],
            ExpectedResponseTimeMs = int.TryParse(configuration["TestConfiguration:ExpectedResponseTimeMs"], out var responseTime) ? responseTime : testConfig.ExpectedResponseTimeMs,
            MaxErrorRatePercent = double.TryParse(configuration["TestConfiguration:MaxErrorRatePercent"], out var errorRate) ? errorRate : testConfig.MaxErrorRatePercent,
            ScenarioName = configuration["TestConfiguration:ScenarioName"]
        };

        // Override with command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--scenario" when i + 1 < args.Length:
                    testConfig = testConfig with { ScenarioName = args[++i] };
                    break;
                case "--name" when i + 1 < args.Length:
                    testConfig = testConfig with { Name = args[++i] };
                    break;
                case "--duration" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var durationValue))
                        testConfig = testConfig with { DurationSeconds = durationValue };
                    break;
                case "--users" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var usersValue))
                        testConfig = testConfig with { ConcurrentUsers = usersValue };
                    break;
                case "--endpoint" when i + 1 < args.Length:
                    testConfig = testConfig with { ApiEndpoint = args[++i] };
                    break;
                case "--method" when i + 1 < args.Length:
                    testConfig = testConfig with { ApiMethod = args[++i] };
                    break;
                case "--sql-connection" when i + 1 < args.Length:
                    testConfig = testConfig with { SqlConnectionString = args[++i] };
                    break;
                case "--sql-procedure" when i + 1 < args.Length:
                    testConfig = testConfig with { SqlProcedureName = args[++i] };
                    break;
                case "--test-type" when i + 1 < args.Length:
                    if (Enum.TryParse<TestType>(args[++i], true, out var testTypeValue))
                        testConfig = testConfig with { TestType = testTypeValue };
                    break;
            }
        }

        return testConfig;
    }

    private static void OutputTeamCityResults(List<Core.Entities.TestResult> results)
    {
        // Output individual test results FIRST
        foreach (var result in results)
        {
            if (result.Status == Core.Enums.TestStatus.Completed && result.ErrorRatePercent <= result.MaxErrorRatePercent)
            {
                Console.WriteLine($"##teamcity[testResult name='{result.TestName}' status='SUCCESS']");
            }
            else
            {
                Console.WriteLine($"##teamcity[testResult name='{result.TestName}' status='FAILURE']");
            }
        }
        
        // Then output aggregated build statistics
        var totalRequests = results.Sum(r => r.TotalRequests);
        var totalSuccessfulRequests = results.Sum(r => r.SuccessfulRequests);
        var totalFailedRequests = results.Sum(r => r.FailedRequests);
        var averageResponseTimeMs = results.Any() ? results.Average(r => r.AverageResponseTimeMs) : 0;
        var averageRequestsPerSecond = results.Any() ? results.Average(r => r.RequestsPerSecond) : 0;
        var averageCpuUsage = results.Any() ? results.Average(r => r.CpuUsagePercent) : 0;
        var averageMemoryUsage = results.Any() ? results.Average(r => r.MemoryUsagePercent) : 0;
        var maxPerformanceImpact = results.Any() ? results.Max(r => r.PerformanceImpact) : Core.Enums.PerformanceImpactLevel.Minor;
        var overallErrorRate = totalRequests > 0 ? (double)totalFailedRequests / totalRequests * 100 : 0;

        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.TotalRequests' value='{totalRequests}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.SuccessfulRequests' value='{totalSuccessfulRequests}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.FailedRequests' value='{totalFailedRequests}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.ErrorRatePercent' value='{overallErrorRate:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.AverageResponseTimeMs' value='{averageResponseTimeMs:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.AverageRequestsPerSecond' value='{averageRequestsPerSecond:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.AverageCpuUsagePercent' value='{averageCpuUsage:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.AverageMemoryUsagePercent' value='{averageMemoryUsage:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.MaxPerformanceImpact' value='{maxPerformanceImpact}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.TotalScenarios' value='{results.Count}']");
        
        // Output overall build status
        var failedTests = results.Count(r => r.Status != Core.Enums.TestStatus.Completed || r.ErrorRatePercent > r.MaxErrorRatePercent);
        var hasMajorPerformanceImpact = results.Any(r => r.PerformanceImpact >= Core.Enums.PerformanceImpactLevel.Major);
        
        if (failedTests > 0 || hasMajorPerformanceImpact)
        {
            var failureReason = failedTests > 0 ? $"{failedTests} test(s) failed" : "Performance degradation detected";
            Console.WriteLine($"##teamcity[buildStatus status='FAILURE' text='{failureReason}']");
        }
        else
        {
            Console.WriteLine($"##teamcity[buildStatus status='SUCCESS' text='All {results.Count} performance tests completed successfully']");
        }
    }
}
