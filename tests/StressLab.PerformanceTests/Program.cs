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

            // Execute test based on configuration
            TestResult result;
            
            if (!string.IsNullOrEmpty(testConfig.ScenarioName))
            {
                // Execute scenario by name
                logger.LogInformation("Executing scenario: {ScenarioName}", testConfig.ScenarioName);
                result = await scenarioExecutionService.ExecuteScenarioByNameAsync(testConfig.ScenarioName);
            }
            else
            {
                // Execute legacy test configuration
                logger.LogInformation("Executing legacy test: {TestName}", testConfig.Name);
                
                var mediator = host.Services.GetRequiredService<MediatR.IMediator>();
                
                // Create test configuration first
                var createCommand = new CreateTestConfigurationCommand
                {
                    Configuration = testConfig
                };
                
                var createdConfig = await mediator.Send(createCommand);

                // Execute the test
                var executeCommand = new ExecutePerformanceTestCommand
                {
                    ConfigurationId = createdConfig.Id
                };
                
                result = await mediator.Send(executeCommand);
            }

            logger.LogInformation("Test completed successfully. Result ID: {ResultId}", result.Id);

            // Generate reports
            var reportsPath = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            Directory.CreateDirectory(reportsPath);

            var htmlReportPath = await reportService.GenerateHtmlReportAsync(result, reportsPath);
            var jsonReportPath = await reportService.GenerateJsonReportAsync(result, reportsPath);
            var csvReportPath = await reportService.GenerateCsvReportAsync(result, reportsPath);

            logger.LogInformation("Reports generated:");
            logger.LogInformation("HTML: {HtmlReport}", htmlReportPath);
            logger.LogInformation("JSON: {JsonReport}", jsonReportPath);
            logger.LogInformation("CSV: {CsvReport}", csvReportPath);

            // Output results for TeamCity
            OutputTeamCityResults(result);

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
        // Default configuration
        var testConfig = new TestConfigurationDto
        {
            Name = "Default Performance Test",
            Description = "Performance test executed from command line",
            TestType = TestType.Api,
            DurationSeconds = 60,
            ConcurrentUsers = 10,
            RampUpSeconds = 10,
            ApiEndpoint = "https://httpbin.org/get",
            ApiMethod = "GET",
            ExpectedResponseTimeMs = 1000,
            MaxErrorRatePercent = 5.0,
            ScenarioName = null
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

    private static void OutputTeamCityResults(Core.Entities.TestResult result)
    {
        // Output TeamCity service messages for build statistics
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.TotalRequests' value='{result.TotalRequests}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.SuccessfulRequests' value='{result.SuccessfulRequests}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.FailedRequests' value='{result.FailedRequests}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.ErrorRatePercent' value='{result.ErrorRatePercent:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.AverageResponseTimeMs' value='{result.AverageResponseTimeMs:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.P95ResponseTimeMs' value='{result.P95ResponseTimeMs:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.P99ResponseTimeMs' value='{result.P99ResponseTimeMs:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.RequestsPerSecond' value='{result.RequestsPerSecond:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.CpuUsagePercent' value='{result.CpuUsagePercent:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.MemoryUsagePercent' value='{result.MemoryUsagePercent:F2}']");
        Console.WriteLine($"##teamcity[buildStatisticValue key='PerformanceTest.PerformanceImpact' value='{result.PerformanceImpact}']");
        
        // Output test result status
        if (result.Status == Core.Enums.TestStatus.Completed && result.ErrorRatePercent <= result.MaxErrorRatePercent)
        {
            Console.WriteLine($"##teamcity[testResult name='{result.TestName}' status='SUCCESS']");
        }
        else
        {
            Console.WriteLine($"##teamcity[testResult name='{result.TestName}' status='FAILURE']");
        }
        
        // Output build status
        if (result.PerformanceImpact >= Core.Enums.PerformanceImpactLevel.Major)
        {
            Console.WriteLine($"##teamcity[buildStatus status='FAILURE' text='Performance degradation detected: {result.PerformanceImpact}']");
        }
        else
        {
            Console.WriteLine($"##teamcity[buildStatus status='SUCCESS' text='Performance test completed successfully']");
        }
    }
}
