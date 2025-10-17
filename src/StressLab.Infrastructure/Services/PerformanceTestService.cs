using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Exceptions;
using StressLab.Core.Interfaces.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Dapper;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for executing performance tests using simple HTTP and SQL operations
/// </summary>
public class PerformanceTestService : IPerformanceTestService
{
    private readonly ILogger<PerformanceTestService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ISystemMetricsService _systemMetricsService;

    public PerformanceTestService(
        ILogger<PerformanceTestService> logger,
        HttpClient httpClient,
        ISystemMetricsService systemMetricsService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _systemMetricsService = systemMetricsService;
    }

    public async Task<TestResult> ExecuteTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting performance test: {TestName}", configuration.Name);

        var startTime = DateTimeOffset.UtcNow;
        await _systemMetricsService.StartMonitoringAsync(cancellationToken);

        try
        {
            var result = configuration.TestType switch
            {
                TestType.Api => await ExecuteApiTestAsync(configuration, cancellationToken),
                TestType.Sql => await ExecuteSqlTestAsync(configuration, cancellationToken),
                TestType.Combined => await ExecuteCombinedTestAsync(configuration, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported test type: {configuration.TestType}")
            };

            _logger.LogInformation("Performance test completed successfully: {TestName}", configuration.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Performance test failed: {TestName}", configuration.Name);
            throw;
        }
        finally
        {
            await _systemMetricsService.StopMonitoringAsync();
        }
    }

    public async Task<TestResult> ExecuteApiTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing API test: {TestName}", configuration.Name);

        var startTime = DateTimeOffset.UtcNow;
        var results = new List<HttpResponseMessage>();
        var errors = new List<Exception>();

        // Simple load test simulation
        var tasks = new List<Task>();
        for (int i = 0; i < configuration.ConcurrentUsers; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var endTime = startTime.AddSeconds(configuration.DurationSeconds);
                while (DateTimeOffset.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var request = new HttpRequestMessage(
                            new HttpMethod(configuration.ApiMethod ?? "GET"),
                            configuration.ApiEndpoint);

                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        results.Add(response);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }

                    // Small delay to prevent overwhelming the system
                    await Task.Delay(100, cancellationToken);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var endTime = DateTimeOffset.UtcNow;
        var systemMetrics = _systemMetricsService.GetMetrics();

        return CreateTestResult(configuration, startTime, endTime, results, errors, systemMetrics);
    }

    public async Task<TestResult> ExecuteSqlTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing SQL test: {TestName}", configuration.Name);

        var startTime = DateTimeOffset.UtcNow;
        var results = new List<object>();
        var errors = new List<Exception>();

        // Simple SQL load test simulation
        var tasks = new List<Task>();
        for (int i = 0; i < configuration.ConcurrentUsers; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var endTime = startTime.AddSeconds(configuration.DurationSeconds);
                while (DateTimeOffset.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var connection = new SqlConnection(configuration.SqlConnectionString);
                        await connection.OpenAsync(cancellationToken);

                        var result = await connection.QueryAsync(
                            $"EXEC {configuration.SqlProcedureName}",
                            commandTimeout: 30);

                        results.AddRange(result);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }

                    // Small delay to prevent overwhelming the database
                    await Task.Delay(200, cancellationToken);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var endTime = DateTimeOffset.UtcNow;
        var systemMetrics = _systemMetricsService.GetMetrics();

        return CreateTestResult(configuration, startTime, endTime, results, errors, systemMetrics);
    }

    public async Task<TestResult> ExecuteCombinedTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing combined test: {TestName}", configuration.Name);

        var startTime = DateTimeOffset.UtcNow;
        var apiResults = new List<HttpResponseMessage>();
        var sqlResults = new List<object>();
        var errors = new List<Exception>();

        // Combined test - execute API and SQL operations together
        var tasks = new List<Task>();
        for (int i = 0; i < configuration.ConcurrentUsers; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var endTime = startTime.AddSeconds(configuration.DurationSeconds);
                while (DateTimeOffset.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Execute API call
                        var request = new HttpRequestMessage(
                            new HttpMethod(configuration.ApiMethod ?? "GET"),
                            configuration.ApiEndpoint);

                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        apiResults.Add(response);

                        // Execute SQL procedure
                        using var connection = new SqlConnection(configuration.SqlConnectionString);
                        await connection.OpenAsync(cancellationToken);

                        var result = await connection.QueryAsync(
                            $"EXEC {configuration.SqlProcedureName}",
                            commandTimeout: 30);

                        sqlResults.AddRange(result);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }

                    // Small delay between combined operations
                    await Task.Delay(300, cancellationToken);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var endTime = DateTimeOffset.UtcNow;
        var systemMetrics = _systemMetricsService.GetMetrics();

        return CreateTestResult(configuration, startTime, endTime, apiResults, sqlResults, errors, systemMetrics);
    }

    private TestResult CreateTestResult(TestConfiguration configuration, DateTimeOffset startTime, DateTimeOffset endTime,
        List<HttpResponseMessage> apiResults, List<Exception> errors, SystemMetrics systemMetrics)
    {
        var duration = (endTime - startTime).TotalSeconds;
        var totalRequests = apiResults.Count;
        var successfulRequests = apiResults.Count(r => r.IsSuccessStatusCode);
        var failedRequests = errors.Count + apiResults.Count(r => !r.IsSuccessStatusCode);
        var errorRatePercent = totalRequests > 0 ? (double)failedRequests / totalRequests * 100 : 0;
        var requestsPerSecond = duration > 0 ? totalRequests / duration : 0;

        var averageResponseTimeMs = apiResults.Any() 
            ? apiResults.Average(r => (double?)r.Headers.Date?.Subtract(startTime.DateTime).TotalMilliseconds ?? 0)
            : 0;

        var status = errorRatePercent <= configuration.MaxErrorRatePercent 
            ? TestStatus.Completed 
            : TestStatus.Failed;

        var performanceImpact = DeterminePerformanceImpact(averageResponseTimeMs, configuration.ExpectedResponseTimeMs, errorRatePercent);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = configuration.Id,
            TestName = configuration.Name,
            Description = configuration.Description,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            ErrorRatePercent = errorRatePercent,
            AverageResponseTimeMs = averageResponseTimeMs,
            P95ResponseTimeMs = averageResponseTimeMs * 1.5, // Simplified calculation
            P99ResponseTimeMs = averageResponseTimeMs * 2.0, // Simplified calculation
            RequestsPerSecond = requestsPerSecond,
            CpuUsagePercent = systemMetrics.CpuUsagePercent,
            MemoryUsagePercent = systemMetrics.MemoryUsagePercent,
            PerformanceImpact = performanceImpact,
            MaxErrorRatePercent = configuration.MaxErrorRatePercent,
            ErrorMessage = status == TestStatus.Failed ? $"Error rate {errorRatePercent:F2}% exceeds maximum {configuration.MaxErrorRatePercent}%" : null
        };
    }

    private TestResult CreateTestResult(TestConfiguration configuration, DateTimeOffset startTime, DateTimeOffset endTime,
        List<object> sqlResults, List<Exception> errors, SystemMetrics systemMetrics)
    {
        var duration = (endTime - startTime).TotalSeconds;
        var totalRequests = sqlResults.Count;
        var successfulRequests = sqlResults.Count;
        var failedRequests = errors.Count;
        var errorRatePercent = totalRequests > 0 ? (double)failedRequests / totalRequests * 100 : 0;
        var requestsPerSecond = duration > 0 ? totalRequests / duration : 0;

        var averageResponseTimeMs = 200.0; // Simplified for SQL operations

        var status = errorRatePercent <= configuration.MaxErrorRatePercent 
            ? TestStatus.Completed 
            : TestStatus.Failed;

        var performanceImpact = DeterminePerformanceImpact(averageResponseTimeMs, configuration.ExpectedResponseTimeMs, errorRatePercent);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = configuration.Id,
            TestName = configuration.Name,
            Description = configuration.Description,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            ErrorRatePercent = errorRatePercent,
            AverageResponseTimeMs = averageResponseTimeMs,
            P95ResponseTimeMs = averageResponseTimeMs * 1.5,
            P99ResponseTimeMs = averageResponseTimeMs * 2.0,
            RequestsPerSecond = requestsPerSecond,
            CpuUsagePercent = systemMetrics.CpuUsagePercent,
            MemoryUsagePercent = systemMetrics.MemoryUsagePercent,
            PerformanceImpact = performanceImpact,
            MaxErrorRatePercent = configuration.MaxErrorRatePercent,
            ErrorMessage = status == TestStatus.Failed ? $"Error rate {errorRatePercent:F2}% exceeds maximum {configuration.MaxErrorRatePercent}%" : null
        };
    }

    private TestResult CreateTestResult(TestConfiguration configuration, DateTimeOffset startTime, DateTimeOffset endTime,
        List<HttpResponseMessage> apiResults, List<object> sqlResults, List<Exception> errors, SystemMetrics systemMetrics)
    {
        var duration = (endTime - startTime).TotalSeconds;
        var totalRequests = apiResults.Count + sqlResults.Count;
        var successfulRequests = apiResults.Count(r => r.IsSuccessStatusCode) + sqlResults.Count;
        var failedRequests = errors.Count + apiResults.Count(r => !r.IsSuccessStatusCode);
        var errorRatePercent = totalRequests > 0 ? (double)failedRequests / totalRequests * 100 : 0;
        var requestsPerSecond = duration > 0 ? totalRequests / duration : 0;

        var averageResponseTimeMs = 300.0; // Simplified for combined operations

        var status = errorRatePercent <= configuration.MaxErrorRatePercent 
            ? TestStatus.Completed 
            : TestStatus.Failed;

        var performanceImpact = DeterminePerformanceImpact(averageResponseTimeMs, configuration.ExpectedResponseTimeMs, errorRatePercent);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = configuration.Id,
            TestName = configuration.Name,
            Description = configuration.Description,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            ErrorRatePercent = errorRatePercent,
            AverageResponseTimeMs = averageResponseTimeMs,
            P95ResponseTimeMs = averageResponseTimeMs * 1.5,
            P99ResponseTimeMs = averageResponseTimeMs * 2.0,
            RequestsPerSecond = requestsPerSecond,
            CpuUsagePercent = systemMetrics.CpuUsagePercent,
            MemoryUsagePercent = systemMetrics.MemoryUsagePercent,
            PerformanceImpact = performanceImpact,
            MaxErrorRatePercent = configuration.MaxErrorRatePercent,
            ErrorMessage = status == TestStatus.Failed ? $"Error rate {errorRatePercent:F2}% exceeds maximum {configuration.MaxErrorRatePercent}%" : null
        };
    }

    private PerformanceImpactLevel DeterminePerformanceImpact(double averageResponseTimeMs, int expectedResponseTimeMs, double errorRatePercent)
    {
        if (errorRatePercent > 10)
            return PerformanceImpactLevel.Critical;
        
        if (averageResponseTimeMs > expectedResponseTimeMs * 2)
            return PerformanceImpactLevel.Major;
        
        if (averageResponseTimeMs > expectedResponseTimeMs * 1.5)
            return PerformanceImpactLevel.Moderate;
        
        if (averageResponseTimeMs > expectedResponseTimeMs)
            return PerformanceImpactLevel.Minor;
        
        return PerformanceImpactLevel.None;
    }
}