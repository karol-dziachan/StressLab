using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StressLab.Application.Commands;
using StressLab.Application.DTOs;
using StressLab.Core.Enums;
using StressLab.Infrastructure.Configuration;

namespace StressLab.PerformanceTests.Examples;

/// <summary>
/// Example API performance test scenario
/// </summary>
public class ApiPerformanceTestExample
{
    public static async Task RunAsync()
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            // Build host
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddStressLabInfrastructure();
                    services.AddStressLabApplication();
                    services.AddStressLabLogging();
                })
                .UseSerilog()
                .Build();

            var mediator = host.Services.GetRequiredService<MediatR.IMediator>();
            var logger = host.Services.GetRequiredService<ILogger<ApiPerformanceTestExample>>();

            // Create test configuration
            var testConfig = new TestConfigurationDto
            {
                Name = "API Performance Test Example",
                Description = "Example API performance test using httpbin.org",
                TestType = TestType.Api,
                DurationSeconds = 60,
                ConcurrentUsers = 20,
                RampUpSeconds = 10,
                ApiEndpoint = "https://httpbin.org/get",
                ApiMethod = "GET",
                ExpectedResponseTimeMs = 1000,
                MaxErrorRatePercent = 5.0
            };

            logger.LogInformation("Starting API performance test: {TestName}", testConfig.Name);

            // Execute test
            var result = await mediator.Send(new ExecutePerformanceTestCommand
            {
                ConfigurationId = Guid.NewGuid() // In real scenario, this would be a saved configuration ID
            });

            logger.LogInformation("Test completed successfully!");
            logger.LogInformation("Total requests: {TotalRequests}", result.TotalRequests);
            logger.LogInformation("Success rate: {SuccessRate:F2}%", 100 - result.ErrorRatePercent);
            logger.LogInformation("Average response time: {AvgResponseTime:F2}ms", result.AverageResponseTimeMs);
            logger.LogInformation("Throughput: {Throughput:F2} req/s", result.RequestsPerSecond);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "API performance test failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

/// <summary>
/// Example SQL performance test scenario
/// </summary>
public class SqlPerformanceTestExample
{
    public static async Task RunAsync()
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            // Build host
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddStressLabInfrastructure();
                    services.AddStressLabApplication();
                    services.AddStressLabLogging();
                })
                .UseSerilog()
                .Build();

            var mediator = host.Services.GetRequiredService<MediatR.IMediator>();
            var logger = host.Services.GetRequiredService<ILogger<SqlPerformanceTestExample>>();

            // Create test configuration
            var testConfig = new TestConfigurationDto
            {
                Name = "SQL Performance Test Example",
                Description = "Example SQL stored procedure performance test",
                TestType = TestType.Sql,
                DurationSeconds = 120,
                ConcurrentUsers = 15,
                RampUpSeconds = 15,
                SqlConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;",
                SqlProcedureName = "sp_GetTestData",
                SqlParameters = new Dictionary<string, object>
                {
                    { "@Param1", "test_value" },
                    { "@Param2", 123 }
                },
                ExpectedResponseTimeMs = 500,
                MaxErrorRatePercent = 2.0
            };

            logger.LogInformation("Starting SQL performance test: {TestName}", testConfig.Name);

            // Execute test
            var result = await mediator.Send(new ExecutePerformanceTestCommand
            {
                ConfigurationId = Guid.NewGuid() // In real scenario, this would be a saved configuration ID
            });

            logger.LogInformation("SQL test completed successfully!");
            logger.LogInformation("Total executions: {TotalExecutions}", result.TotalRequests);
            logger.LogInformation("Success rate: {SuccessRate:F2}%", 100 - result.ErrorRatePercent);
            logger.LogInformation("Average execution time: {AvgExecutionTime:F2}ms", result.AverageResponseTimeMs);
            
            if (result.SqlMetrics is not null)
            {
                logger.LogInformation("SQL-specific metrics:");
                logger.LogInformation("  - Execution count: {ExecutionCount}", result.SqlMetrics.ExecutionCount);
                logger.LogInformation("  - Error count: {ErrorCount}", result.SqlMetrics.ErrorCount);
                logger.LogInformation("  - Active connections: {ActiveConnections}", result.SqlMetrics.ActiveConnections);
                logger.LogInformation("  - Deadlock count: {DeadlockCount}", result.SqlMetrics.DeadlockCount);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "SQL performance test failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

/// <summary>
/// Example combined API and SQL performance test scenario
/// </summary>
public class CombinedPerformanceTestExample
{
    public static async Task RunAsync()
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            // Build host
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddStressLabInfrastructure();
                    services.AddStressLabApplication();
                    services.AddStressLabLogging();
                })
                .UseSerilog()
                .Build();

            var mediator = host.Services.GetRequiredService<MediatR.IMediator>();
            var logger = host.Services.GetRequiredService<ILogger<CombinedPerformanceTestExample>>();

            // Create test configuration
            var testConfig = new TestConfigurationDto
            {
                Name = "Combined API and SQL Performance Test",
                Description = "Simultaneous API and SQL performance test to measure system impact",
                TestType = TestType.Combined,
                DurationSeconds = 180,
                ConcurrentUsers = 25,
                RampUpSeconds = 20,
                ApiEndpoint = "https://httpbin.org/get",
                ApiMethod = "GET",
                SqlConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;",
                SqlProcedureName = "sp_GetTestData",
                SqlParameters = new Dictionary<string, object>
                {
                    { "@Param1", "test_value" },
                    { "@Param2", 123 }
                },
                ExpectedResponseTimeMs = 1500,
                MaxErrorRatePercent = 7.5
            };

            logger.LogInformation("Starting combined performance test: {TestName}", testConfig.Name);

            // Execute test
            var result = await mediator.Send(new ExecutePerformanceTestCommand
            {
                ConfigurationId = Guid.NewGuid() // In real scenario, this would be a saved configuration ID
            });

            logger.LogInformation("Combined test completed successfully!");
            logger.LogInformation("Total requests: {TotalRequests}", result.TotalRequests);
            logger.LogInformation("Success rate: {SuccessRate:F2}%", 100 - result.ErrorRatePercent);
            logger.LogInformation("Average response time: {AvgResponseTime:F2}ms", result.AverageResponseTimeMs);
            logger.LogInformation("Throughput: {Throughput:F2} req/s", result.RequestsPerSecond);
            logger.LogInformation("Performance impact: {PerformanceImpact}", result.PerformanceImpact);
            
            if (result.SqlMetrics is not null)
            {
                logger.LogInformation("SQL metrics:");
                logger.LogInformation("  - Execution count: {ExecutionCount}", result.SqlMetrics.ExecutionCount);
                logger.LogInformation("  - Average execution time: {AvgExecutionTime:F2}ms", result.SqlMetrics.AverageExecutionTimeMs);
                logger.LogInformation("  - Database CPU usage: {DbCpuUsage:F2}%", result.SqlMetrics.DatabaseCpuUsagePercent);
                logger.LogInformation("  - Database memory usage: {DbMemoryUsage:F2}%", result.SqlMetrics.DatabaseMemoryUsagePercent);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Combined performance test failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

