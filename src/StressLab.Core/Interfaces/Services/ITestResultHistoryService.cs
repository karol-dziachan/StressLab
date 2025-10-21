using StressLab.Core.Entities;
using StressLab.Core.Interfaces.Repositories;

namespace StressLab.Core.Interfaces.Services;

/// <summary>
/// Service interface for managing test result history and performance analysis
/// </summary>
public interface ITestResultHistoryService
{
    /// <summary>
    /// Logs a test result to history
    /// </summary>
    /// <param name="testResult">Test result to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the logging operation</returns>
    Task LogTestResultAsync(TestResult testResult, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes performance deviation for a test result compared to historical baseline
    /// </summary>
    /// <param name="testResult">Current test result</param>
    /// <param name="sampleSize">Number of historical samples to use for baseline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance analysis or null if insufficient historical data</returns>
    Task<PerformanceAnalysis?> AnalyzePerformanceDeviationAsync(TestResult testResult, int sampleSize = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets performance trend for a test name
    /// </summary>
    /// <param name="testName">Test name</param>
    /// <param name="days">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance trend analysis</returns>
    Task<PerformanceAnalysis?> GetPerformanceTrendAsync(string testName, int days = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets baseline statistics for a test name
    /// </summary>
    /// <param name="testName">Test name</param>
    /// <param name="sampleSize">Number of recent tests to use for baseline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Baseline statistics or null if insufficient data</returns>
    Task<TestResultHistory?> GetBaselineAsync(string testName, int sampleSize = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up old historical records
    /// </summary>
    /// <param name="retentionDays">Number of days to retain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records cleaned up</returns>
    Task<int> CleanupOldRecordsAsync(int retentionDays = 90, CancellationToken cancellationToken = default);
}
