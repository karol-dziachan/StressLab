using Microsoft.Extensions.Logging;
using StressLab.Core.Entities;
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

