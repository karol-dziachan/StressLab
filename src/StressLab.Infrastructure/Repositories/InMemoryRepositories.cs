using Microsoft.Extensions.Logging;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Interfaces.Repositories;
using System.Collections.Concurrent;

namespace StressLab.Infrastructure.Repositories;

/// <summary>
/// In-memory repository for test configurations
/// </summary>
public class InMemoryTestConfigurationRepository : ITestConfigurationRepository
{
    private readonly ConcurrentDictionary<Guid, TestConfiguration> _configurations = new();
    private readonly ILogger<InMemoryTestConfigurationRepository> _logger;

    public InMemoryTestConfigurationRepository(ILogger<InMemoryTestConfigurationRepository> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TestConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all test configurations");
        
        await Task.CompletedTask; // Simulate async operation
        
        return _configurations.Values.ToList();
    }

    public async Task<TestConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving test configuration: {Id}", id);
        
        await Task.CompletedTask; // Simulate async operation
        
        _configurations.TryGetValue(id, out var configuration);
        return configuration;
    }

    public async Task<TestConfiguration> CreateAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating test configuration: {Id}", configuration.Id);
        
        await Task.CompletedTask; // Simulate async operation
        
        _configurations.TryAdd(configuration.Id, configuration);
        
        _logger.LogDebug("Test configuration created successfully: {Id}", configuration.Id);
        
        return configuration;
    }

    public async Task<TestConfiguration> UpdateAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating test configuration: {Id}", configuration.Id);
        
        await Task.CompletedTask; // Simulate async operation
        
        _configurations.AddOrUpdate(configuration.Id, configuration, (key, existing) => configuration);
        
        _logger.LogDebug("Test configuration updated successfully: {Id}", configuration.Id);
        
        return configuration;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting test configuration: {Id}", id);
        
        await Task.CompletedTask; // Simulate async operation
        
        var removed = _configurations.TryRemove(id, out _);
        
        _logger.LogDebug("Test configuration deletion result: {Removed} for ID: {Id}", removed, id);
        
        return removed;
    }
}

/// <summary>
/// In-memory repository for test results
/// </summary>
public class InMemoryTestResultRepository : ITestResultRepository
{
    private readonly ConcurrentDictionary<Guid, TestResult> _results = new();
    private readonly ILogger<InMemoryTestResultRepository> _logger;

    public InMemoryTestResultRepository(ILogger<InMemoryTestResultRepository> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TestResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all test results");
        
        await Task.CompletedTask; // Simulate async operation
        
        return _results.Values.OrderByDescending(r => r.StartTime).ToList();
    }

    public async Task<IEnumerable<TestResult>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving test results for configuration: {ConfigurationId}", configurationId);
        
        await Task.CompletedTask; // Simulate async operation
        
        var results = _results.Values
            .Where(r => r.TestConfigurationId == configurationId)
            .OrderByDescending(r => r.StartTime)
            .ToList();
        
        _logger.LogDebug("Found {Count} test results for configuration: {ConfigurationId}", results.Count, configurationId);
        
        return results;
    }

    public async Task<TestResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving test result: {Id}", id);
        
        await Task.CompletedTask; // Simulate async operation
        
        _results.TryGetValue(id, out var result);
        return result;
    }

    public async Task<TestResult> CreateAsync(TestResult result, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating test result: {Id}", result.Id);
        
        await Task.CompletedTask; // Simulate async operation
        
        _results.TryAdd(result.Id, result);
        
        _logger.LogDebug("Test result created successfully: {Id}", result.Id);
        
        return result;
    }

    public async Task<TestResult> UpdateAsync(TestResult result, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating test result: {Id}", result.Id);
        
        await Task.CompletedTask; // Simulate async operation
        
        _results.AddOrUpdate(result.Id, result, (key, existing) => result);
        
        _logger.LogDebug("Test result updated successfully: {Id}", result.Id);
        
        return result;
    }
}

/// <summary>
/// In-memory repository for test result history
/// </summary>
public class InMemoryTestResultHistoryRepository : ITestResultHistoryRepository
{
    private readonly ConcurrentDictionary<Guid, TestResultHistory> _history = new();
    private readonly ILogger<InMemoryTestResultHistoryRepository> _logger;

    public InMemoryTestResultHistoryRepository(ILogger<InMemoryTestResultHistoryRepository> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TestResultHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all historical test results");
        
        await Task.CompletedTask; // Simulate async operation
        
        return _history.Values.OrderByDescending(h => h.ExecutionDate).ToList();
    }

    public async Task<IEnumerable<TestResultHistory>> GetByTestNameAsync(string testName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving historical test results for test: {TestName}", testName);
        
        await Task.CompletedTask; // Simulate async operation
        
        var results = _history.Values
            .Where(h => h.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.ExecutionDate)
            .ToList();
        
        _logger.LogDebug("Found {Count} historical results for test: {TestName}", results.Count, testName);
        
        return results;
    }

    public async Task<IEnumerable<TestResultHistory>> GetByTestNameAndDateRangeAsync(string testName, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving historical test results for test: {TestName} from {FromDate} to {ToDate}", testName, fromDate, toDate);
        
        await Task.CompletedTask; // Simulate async operation
        
        var results = _history.Values
            .Where(h => h.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase) &&
                       h.ExecutionDate >= fromDate && h.ExecutionDate <= toDate)
            .OrderByDescending(h => h.ExecutionDate)
            .ToList();
        
        _logger.LogDebug("Found {Count} historical results for test: {TestName} in date range", results.Count, testName);
        
        return results;
    }

    public async Task<IEnumerable<TestResultHistory>> GetRecentByTestNameAsync(string testName, int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving {Count} recent historical test results for test: {TestName}", count, testName);
        
        await Task.CompletedTask; // Simulate async operation
        
        var results = _history.Values
            .Where(h => h.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.ExecutionDate)
            .Take(count)
            .ToList();
        
        _logger.LogDebug("Found {Count} recent historical results for test: {TestName}", results.Count, testName);
        
        return results;
    }

    public async Task<TestResultHistory> CreateAsync(TestResultHistory history, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating historical test result: {Id} for test: {TestName}", history.Id, history.TestName);
        
        await Task.CompletedTask; // Simulate async operation
        
        _history.TryAdd(history.Id, history);
        
        _logger.LogDebug("Historical test result created successfully: {Id}", history.Id);
        
        return history;
    }

    public async Task<TestResultHistory?> GetBaselineAsync(string testName, int sampleSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calculating baseline for test: {TestName} using {SampleSize} samples", testName, sampleSize);
        
        await Task.CompletedTask; // Simulate async operation
        
        var recentResults = _history.Values
            .Where(h => h.TestName.Equals(testName, StringComparison.OrdinalIgnoreCase) &&
                       h.Status == TestStatus.Completed)
            .OrderByDescending(h => h.ExecutionDate)
            .Take(sampleSize)
            .ToList();
        
        if (recentResults.Count < 3) // Need at least 3 samples for meaningful baseline
        {
            _logger.LogDebug("Insufficient data for baseline calculation: {Count} samples", recentResults.Count);
            return null;
        }
        
        // Calculate average values for baseline
        var baseline = new TestResultHistory
        {
            Id = Guid.NewGuid(),
            TestName = testName,
            ExecutionDate = DateTime.UtcNow,
            DurationSeconds = recentResults.Average(r => r.DurationSeconds),
            TotalRequests = (long)recentResults.Average(r => r.TotalRequests),
            SuccessfulRequests = (long)recentResults.Average(r => r.SuccessfulRequests),
            FailedRequests = (long)recentResults.Average(r => r.FailedRequests),
            ErrorRatePercent = recentResults.Average(r => r.ErrorRatePercent),
            AverageResponseTimeMs = recentResults.Average(r => r.AverageResponseTimeMs),
            MinResponseTimeMs = recentResults.Average(r => r.MinResponseTimeMs),
            MaxResponseTimeMs = recentResults.Average(r => r.MaxResponseTimeMs),
            P95ResponseTimeMs = recentResults.Average(r => r.P95ResponseTimeMs),
            P99ResponseTimeMs = recentResults.Average(r => r.P99ResponseTimeMs),
            RequestsPerSecond = recentResults.Average(r => r.RequestsPerSecond),
            CpuUsagePercent = recentResults.Average(r => r.CpuUsagePercent),
            MemoryUsagePercent = recentResults.Average(r => r.MemoryUsagePercent),
            PerformanceImpact = recentResults.GroupBy(r => r.PerformanceImpact)
                .OrderByDescending(g => g.Count())
                .First().Key,
            Status = TestStatus.Completed
        };
        
        _logger.LogDebug("Baseline calculated for test: {TestName} using {SampleSize} samples", testName, recentResults.Count);
        
        return baseline;
    }

    public async Task<int> CleanupOldRecordsAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up historical records older than {RetentionDays} days", retentionDays);
        
        await Task.CompletedTask; // Simulate async operation
        
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var keysToRemove = _history
            .Where(kvp => kvp.Value.ExecutionDate < cutoffDate)
            .Select(kvp => kvp.Key)
            .ToList();
        
        var removedCount = 0;
        foreach (var key in keysToRemove)
        {
            if (_history.TryRemove(key, out _))
            {
                removedCount++;
            }
        }
        
        _logger.LogDebug("Cleaned up {RemovedCount} historical records", removedCount);
        
        return removedCount;
    }
}

