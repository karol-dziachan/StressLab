using StressLab.Core.Entities;

namespace StressLab.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing test configurations
/// </summary>
public interface ITestConfigurationRepository
{
    /// <summary>
    /// Gets all test configurations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test configurations</returns>
    Task<IEnumerable<TestConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a test configuration by ID
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test configuration or null if not found</returns>
    Task<TestConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new test configuration
    /// </summary>
    /// <param name="configuration">Configuration to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created configuration</returns>
    Task<TestConfiguration> CreateAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing test configuration
    /// </summary>
    /// <param name="configuration">Configuration to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated configuration</returns>
    Task<TestConfiguration> UpdateAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a test configuration
    /// </summary>
    /// <param name="id">Configuration ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for managing test results
/// </summary>
public interface ITestResultRepository
{
    /// <summary>
    /// Gets all test results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    Task<IEnumerable<TestResult>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets test results by configuration ID
    /// </summary>
    /// <param name="configurationId">Configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test results</returns>
    Task<IEnumerable<TestResult>> GetByConfigurationIdAsync(Guid configurationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a test result by ID
    /// </summary>
    /// <param name="id">Result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result or null if not found</returns>
    Task<TestResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new test result
    /// </summary>
    /// <param name="result">Result to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created result</returns>
    Task<TestResult> CreateAsync(TestResult result, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing test result
    /// </summary>
    /// <param name="result">Result to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated result</returns>
    Task<TestResult> UpdateAsync(TestResult result, CancellationToken cancellationToken = default);
}

