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
    private readonly IHttpClientConfigurationService _httpClientConfigService;
    private readonly FailCriteriaOptions _failCriteria;
    private readonly ITestResultHistoryService? _historyService;

    public PerformanceTestService(
        ILogger<PerformanceTestService> logger,
        HttpClient httpClient,
        ISystemMetricsService systemMetricsService,
        IHttpClientConfigurationService httpClientConfigService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _systemMetricsService = systemMetricsService;
        _httpClientConfigService = httpClientConfigService;
        _failCriteria = new FailCriteriaOptions();
        _historyService = null;
    }

    public PerformanceTestService(
        ILogger<PerformanceTestService> logger,
        HttpClient httpClient,
        ISystemMetricsService systemMetricsService,
        IHttpClientConfigurationService httpClientConfigService,
        FailCriteriaOptions failCriteriaOptions)
    {
        _logger = logger;
        _httpClient = httpClient;
        _systemMetricsService = systemMetricsService;
        _httpClientConfigService = httpClientConfigService;
        _failCriteria = failCriteriaOptions ?? new FailCriteriaOptions();
        _historyService = null;
    }

    public PerformanceTestService(
        ILogger<PerformanceTestService> logger,
        HttpClient httpClient,
        ISystemMetricsService systemMetricsService,
        IHttpClientConfigurationService httpClientConfigService,
        FailCriteriaOptions failCriteriaOptions,
        ITestResultHistoryService historyService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _systemMetricsService = systemMetricsService;
        _httpClientConfigService = httpClientConfigService;
        _failCriteria = failCriteriaOptions ?? new FailCriteriaOptions();
        _historyService = historyService;
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

            // Log test result to history if history service is available
            if (_historyService is not null)
            {
                try
                {
                    await _historyService.LogTestResultAsync(result, cancellationToken);
                    _logger.LogInformation("Test result logged to history: {TestName}", configuration.Name);
                    
                    // TODO: Check for performance deviations and send alerts
                    // After logging to history, analyze deviations and send email alerts if needed
                    // Example integration:
                    // var analysis = await _historyService.AnalyzePerformanceDeviationAsync(result, cancellationToken: cancellationToken);
                    // if (analysis is not null && (analysis.OverallDeviationScore > 20 || analysis.TrendDirection == TrendDirection.Degrading))
                    // {
                    //     await _emailNotificationService.SendPerformanceAlertAsync(result.TestName, analysis, cancellationToken);
                    // }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log test result to history: {TestName}", configuration.Name);
                    // Don't fail the test if history logging fails
                }
            }

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

    /// <summary>
    /// Configures HttpClient with custom settings for performance tests
    /// </summary>
    /// <param name="configuration">HttpClient configuration</param>
    public void ConfigureHttpClient(HttpClientConfiguration configuration)
    {
        _logger.LogInformation("Configuring HttpClient for performance tests");
        _httpClientConfigService.ConfigureHttpClient(_httpClient, configuration);
    }

    public async Task<TestResult> ExecuteApiTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing API test: {TestName}", configuration.Name);

        var startTime = DateTimeOffset.UtcNow;
        var results = new List<HttpResponseMessage>();
        var responseTimesMs = new System.Collections.Concurrent.ConcurrentBag<double>();
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

                        var stopwatch = Stopwatch.StartNew();
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        stopwatch.Stop();
                        responseTimesMs.Add(stopwatch.Elapsed.TotalMilliseconds);
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

        return CreateTestResult(configuration, startTime, endTime, results, responseTimesMs.ToList(), errors, systemMetrics);
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
        var responseTimesMs = new System.Collections.Concurrent.ConcurrentBag<double>();
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

                        var stopwatch = Stopwatch.StartNew();
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        stopwatch.Stop();
                        responseTimesMs.Add(stopwatch.Elapsed.TotalMilliseconds);
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

        return CreateTestResult(configuration, startTime, endTime, apiResults, sqlResults, responseTimesMs.ToList(), errors, systemMetrics);
    }

    private TestResult CreateTestResult(TestConfiguration configuration, DateTimeOffset startTime, DateTimeOffset endTime,
        List<HttpResponseMessage> apiResults, List<double> responseTimesMs, List<Exception> errors, SystemMetrics systemMetrics)
    {
        var duration = (endTime - startTime).TotalSeconds;
        var totalRequests = apiResults.Count;
        var successfulRequests = apiResults.Count(r => r.IsSuccessStatusCode);
        var failedRequests = errors.Count + apiResults.Count(r => !r.IsSuccessStatusCode);
        var errorRatePercent = totalRequests > 0 ? (double)failedRequests / totalRequests * 100 : 0;
        var requestsPerSecond = duration > 0 ? totalRequests / duration : 0;
        var averageResponseTimeMs = responseTimesMs.Any() ? responseTimesMs.Average() : 0;
        var minResponseTimeMs = responseTimesMs.Any() ? responseTimesMs.Min() : 0;
        var maxResponseTimeMs = responseTimesMs.Any() ? responseTimesMs.Max() : 0;
        var p95ResponseTimeMs = responseTimesMs.Any() ? Percentile(responseTimesMs, 0.95) : averageResponseTimeMs * 1.5;
        var p99ResponseTimeMs = responseTimesMs.Any() ? Percentile(responseTimesMs, 0.99) : averageResponseTimeMs * 2.0;

        var status = DetermineStatus(configuration, averageResponseTimeMs, p95ResponseTimeMs, p99ResponseTimeMs, requestsPerSecond, errorRatePercent);

        var performanceImpact = DeterminePerformanceImpact(averageResponseTimeMs, configuration.ExpectedResponseTimeMs, errorRatePercent);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = configuration.Id,
            TestName = configuration.Name,
            Description = configuration.Description,
            StartTime = startTime,
            EndTime = endTime,
            DurationSeconds = duration,
            Status = status,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            ErrorRatePercent = errorRatePercent,
            AverageResponseTimeMs = averageResponseTimeMs,
            MinResponseTimeMs = minResponseTimeMs,
            MaxResponseTimeMs = maxResponseTimeMs,
            P95ResponseTimeMs = p95ResponseTimeMs,
            P99ResponseTimeMs = p99ResponseTimeMs,
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

        var status = DetermineStatus(configuration, averageResponseTimeMs, averageResponseTimeMs * 1.5, averageResponseTimeMs * 2.0, requestsPerSecond, errorRatePercent);

        var performanceImpact = DeterminePerformanceImpact(averageResponseTimeMs, configuration.ExpectedResponseTimeMs, errorRatePercent);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = configuration.Id,
            TestName = configuration.Name,
            Description = configuration.Description,
            StartTime = startTime,
            EndTime = endTime,
            DurationSeconds = duration,
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
        List<HttpResponseMessage> apiResults, List<object> sqlResults, List<double> responseTimesMs, List<Exception> errors, SystemMetrics systemMetrics)
    {
        var duration = (endTime - startTime).TotalSeconds;
        var totalRequests = apiResults.Count + sqlResults.Count;
        var successfulRequests = apiResults.Count(r => r.IsSuccessStatusCode) + sqlResults.Count;
        var failedRequests = errors.Count + apiResults.Count(r => !r.IsSuccessStatusCode);
        var errorRatePercent = totalRequests > 0 ? (double)failedRequests / totalRequests * 100 : 0;
        var requestsPerSecond = duration > 0 ? totalRequests / duration : 0;
        var averageResponseTimeMs = responseTimesMs.Any() ? responseTimesMs.Average() : 300.0;
        var minResponseTimeMs = responseTimesMs.Any() ? responseTimesMs.Min() : averageResponseTimeMs;
        var maxResponseTimeMs = responseTimesMs.Any() ? responseTimesMs.Max() : averageResponseTimeMs;
        var p95ResponseTimeMs = responseTimesMs.Any() ? Percentile(responseTimesMs, 0.95) : averageResponseTimeMs * 1.5;
        var p99ResponseTimeMs = responseTimesMs.Any() ? Percentile(responseTimesMs, 0.99) : averageResponseTimeMs * 2.0;

        var status = DetermineStatus(configuration, averageResponseTimeMs, p95ResponseTimeMs, p99ResponseTimeMs, requestsPerSecond, errorRatePercent);

        var performanceImpact = DeterminePerformanceImpact(averageResponseTimeMs, configuration.ExpectedResponseTimeMs, errorRatePercent);

        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestConfigurationId = configuration.Id,
            TestName = configuration.Name,
            Description = configuration.Description,
            StartTime = startTime,
            EndTime = endTime,
            DurationSeconds = duration,
            Status = status,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            ErrorRatePercent = errorRatePercent,
            AverageResponseTimeMs = averageResponseTimeMs,
            MinResponseTimeMs = minResponseTimeMs,
            MaxResponseTimeMs = maxResponseTimeMs,
            P95ResponseTimeMs = p95ResponseTimeMs,
            P99ResponseTimeMs = p99ResponseTimeMs,
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

    private TestStatus DetermineStatus(TestConfiguration configuration,
        double avgMs, double p95Ms, double p99Ms, double rps, double errorRatePercent)
    {
        var maxError = _failCriteria.MaxErrorRatePercent ?? configuration.MaxErrorRatePercent;
        var maxAvg = _failCriteria.MaxAverageResponseTimeMs ?? configuration.ExpectedResponseTimeMs;
        var maxP95 = _failCriteria.MaxP95ResponseTimeMs ?? (maxAvg * 1.5);
        var maxP99 = _failCriteria.MaxP99ResponseTimeMs ?? (maxAvg * 2.0);
        var minRps = _failCriteria.MinRequestsPerSecond; // optional

        var fails = false;
        if (errorRatePercent > maxError) fails = true;
        if (avgMs > maxAvg) fails = true;
        if (p95Ms > maxP95) fails = true;
        if (p99Ms > maxP99) fails = true;
        if (minRps is not null && rps < minRps) fails = true;

        return fails ? TestStatus.Failed : TestStatus.Completed;
    }

    private static double Percentile(List<double> sequence, double percentile)
    {
        if (sequence is null || sequence.Count == 0) return 0;
        var ordered = sequence.OrderBy(x => x).ToArray();
        var position = (ordered.Length - 1) * percentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex) return ordered[lowerIndex];
        var weight = position - lowerIndex;
        return ordered[lowerIndex] * (1 - weight) + ordered[upperIndex] * weight;
    }
}