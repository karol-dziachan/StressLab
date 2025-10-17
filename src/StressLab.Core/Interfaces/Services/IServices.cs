using StressLab.Core.Entities;

namespace StressLab.Core.Interfaces.Services;

/// <summary>
/// Service interface for executing performance tests
/// </summary>
public interface IPerformanceTestService
{
    /// <summary>
    /// Executes a performance test based on the provided configuration
    /// </summary>
    /// <param name="configuration">Test configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<TestResult> ExecuteTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes an API performance test
    /// </summary>
    /// <param name="configuration">Test configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<TestResult> ExecuteApiTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a SQL performance test
    /// </summary>
    /// <param name="configuration">Test configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<TestResult> ExecuteSqlTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a combined API and SQL performance test
    /// </summary>
    /// <param name="configuration">Test configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<TestResult> ExecuteCombinedTestAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for generating test reports
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates an HTML report for the test result
    /// </summary>
    /// <param name="result">Test result</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the generated report</returns>
    Task<string> GenerateHtmlReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a JSON report for the test result
    /// </summary>
    /// <param name="result">Test result</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the generated report</returns>
    Task<string> GenerateJsonReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a CSV report for the test result
    /// </summary>
    /// <param name="result">Test result</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the generated report</returns>
    Task<string> GenerateCsvReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a combined HTML report for multiple test results
    /// </summary>
    /// <param name="results">Collection of test results</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the generated report</returns>
    Task<string> GenerateCombinedHtmlReportAsync(IEnumerable<TestResult> results, string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for managing test scenarios from JSON configuration
/// </summary>
public interface IScenarioConfigurationService
{
    /// <summary>
    /// Loads scenarios from JSON configuration file
    /// </summary>
    /// <param name="scenariosFilePath">Path to the scenarios JSON file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the loading operation</returns>
    Task LoadScenariosAsync(string scenariosFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a scenario by name
    /// </summary>
    /// <param name="scenarioName">Name of the scenario</param>
    /// <returns>Test scenario or null if not found</returns>
    TestScenario? GetScenario(string scenarioName);
    
    /// <summary>
    /// Gets all loaded scenarios
    /// </summary>
    /// <returns>Collection of test scenarios</returns>
    IEnumerable<TestScenario> GetAllScenarios();
    
    /// <summary>
    /// Gets all scenario names
    /// </summary>
    /// <returns>Collection of scenario names</returns>
    IEnumerable<string> GetScenarioNames();
}

/// <summary>
/// Service interface for executing test scenarios
/// </summary>
public interface IScenarioExecutionService
{
    /// <summary>
    /// Executes a test scenario
    /// </summary>
    /// <param name="scenario">Scenario to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<TestResult> ExecuteScenarioAsync(TestScenario scenario, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a scenario by name
    /// </summary>
    /// <param name="scenarioName">Name of the scenario to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<TestResult> ExecuteScenarioByNameAsync(string scenarioName, CancellationToken cancellationToken = default);
}
public interface ISystemMetricsService
{
    /// <summary>
    /// Starts monitoring system metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the monitoring operation</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops monitoring system metrics
    /// </summary>
    /// <returns>Current system metrics</returns>
    Task<SystemMetrics> StopMonitoringAsync();
    
    /// <summary>
    /// Gets current system metrics
    /// </summary>
    /// <returns>Current system metrics</returns>
    SystemMetrics GetMetrics();
}

/// <summary>
/// Represents system performance metrics
/// </summary>
public record SystemMetrics
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; init; }
    
    /// <summary>
    /// Memory usage percentage
    /// </summary>
    public double MemoryUsagePercent { get; init; }
    
    /// <summary>
    /// Available memory in bytes
    /// </summary>
    public long AvailableMemoryBytes { get; init; }
    
    /// <summary>
    /// Total memory in bytes
    /// </summary>
    public long TotalMemoryBytes { get; init; }
    
    /// <summary>
    /// Disk usage percentage
    /// </summary>
    public double DiskUsagePercent { get; init; }
    
    /// <summary>
    /// Network bytes sent
    /// </summary>
    public long NetworkBytesSent { get; init; }
    
    /// <summary>
    /// Network bytes received
    /// </summary>
    public long NetworkBytesReceived { get; init; }
    
    /// <summary>
    /// Timestamp when metrics were collected
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
