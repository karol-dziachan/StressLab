using StressLab.Core.Enums;

namespace StressLab.Application.DTOs;

/// <summary>
/// Data Transfer Object for test configuration
/// </summary>
public record TestConfigurationDto
{
    /// <summary>
    /// Name of the test configuration
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description of what this test measures
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Type of test to execute
    /// </summary>
    public required TestType TestType { get; init; }
    
    /// <summary>
    /// Duration of the test in seconds
    /// </summary>
    public int DurationSeconds { get; init; } = 60;
    
    /// <summary>
    /// Number of concurrent users/virtual users
    /// </summary>
    public int ConcurrentUsers { get; init; } = 10;
    
    /// <summary>
    /// Ramp-up time in seconds (time to reach full load)
    /// </summary>
    public int RampUpSeconds { get; init; } = 10;
    
    /// <summary>
    /// API endpoint URL to test
    /// </summary>
    public string? ApiEndpoint { get; init; }
    
    /// <summary>
    /// HTTP method for API tests
    /// </summary>
    public string ApiMethod { get; init; } = "GET";
    
    /// <summary>
    /// SQL connection string for database tests
    /// </summary>
    public string? SqlConnectionString { get; init; }
    
    /// <summary>
    /// SQL procedure name to execute
    /// </summary>
    public string? SqlProcedureName { get; init; }
    
    /// <summary>
    /// Parameters for SQL procedure
    /// </summary>
    public Dictionary<string, object>? SqlParameters { get; init; }
    
    /// <summary>
    /// Expected response time threshold in milliseconds
    /// </summary>
    public int ExpectedResponseTimeMs { get; init; } = 1000;
    
    /// <summary>
    /// Maximum acceptable error rate percentage
    /// </summary>
    public double MaxErrorRatePercent { get; init; } = 5.0;
    
    /// <summary>
    /// Scenario name to execute (if using JSON scenarios)
    /// </summary>
    public string? ScenarioName { get; init; }
}

/// <summary>
/// Data Transfer Object for test result
/// </summary>
public record TestResultDto
{
    /// <summary>
    /// Unique identifier for the test result
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// Reference to the test configuration used
    /// </summary>
    public Guid? TestConfigurationId { get; init; }
    
    /// <summary>
    /// Name of the executed test
    /// </summary>
    public required string TestName { get; init; }
    
    /// <summary>
    /// Status of the test execution
    /// </summary>
    public required TestStatus Status { get; init; }
    
    /// <summary>
    /// Start time of the test execution
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }
    
    /// <summary>
    /// End time of the test execution
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }
    
    /// <summary>
    /// Total duration of the test in seconds
    /// </summary>
    public double? DurationSeconds { get; init; }
    
    /// <summary>
    /// Total number of requests executed
    /// </summary>
    public long TotalRequests { get; init; }
    
    /// <summary>
    /// Number of successful requests
    /// </summary>
    public long SuccessfulRequests { get; init; }
    
    /// <summary>
    /// Number of failed requests
    /// </summary>
    public long FailedRequests { get; init; }
    
    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRatePercent { get; init; }
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Minimum response time in milliseconds
    /// </summary>
    public double MinResponseTimeMs { get; init; }
    
    /// <summary>
    /// Maximum response time in milliseconds
    /// </summary>
    public double MaxResponseTimeMs { get; init; }
    
    /// <summary>
    /// 95th percentile response time in milliseconds
    /// </summary>
    public double P95ResponseTimeMs { get; init; }
    
    /// <summary>
    /// 99th percentile response time in milliseconds
    /// </summary>
    public double P99ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Requests per second (throughput)
    /// </summary>
    public double RequestsPerSecond { get; init; }
    
    /// <summary>
    /// System CPU usage percentage during test
    /// </summary>
    public double CpuUsagePercent { get; init; }
    
    /// <summary>
    /// System memory usage percentage during test
    /// </summary>
    public double MemoryUsagePercent { get; init; }
    
    /// <summary>
    /// SQL-specific metrics (if applicable)
    /// </summary>
    public SqlMetricsDto? SqlMetrics { get; init; }
    
    /// <summary>
    /// Performance impact assessment
    /// </summary>
    public PerformanceImpactLevel PerformanceImpact { get; init; }
    
    /// <summary>
    /// Additional notes or observations
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Path to the generated HTML report
    /// </summary>
    public string? ReportPath { get; init; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Data Transfer Object for SQL metrics
/// </summary>
public record SqlMetricsDto
{
    /// <summary>
    /// Average SQL execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Maximum SQL execution time in milliseconds
    /// </summary>
    public double MaxExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Number of SQL executions
    /// </summary>
    public long ExecutionCount { get; init; }
    
    /// <summary>
    /// SQL error count
    /// </summary>
    public long ErrorCount { get; init; }
    
    /// <summary>
    /// Database CPU usage percentage
    /// </summary>
    public double DatabaseCpuUsagePercent { get; init; }
    
    /// <summary>
    /// Database memory usage percentage
    /// </summary>
    public double DatabaseMemoryUsagePercent { get; init; }
    
    /// <summary>
    /// Number of active connections during test
    /// </summary>
    public int ActiveConnections { get; init; }
    
    /// <summary>
    /// Lock wait time in milliseconds
    /// </summary>
    public double LockWaitTimeMs { get; init; }
    
    /// <summary>
    /// Deadlock count
    /// </summary>
    public int DeadlockCount { get; init; }
}

/// <summary>
/// Data Transfer Object for performance analysis
/// </summary>
public record PerformanceAnalysisDto
{
    /// <summary>
    /// Configuration ID being analyzed
    /// </summary>
    public required Guid ConfigurationId { get; init; }
    
    /// <summary>
    /// Configuration name
    /// </summary>
    public required string ConfigurationName { get; init; }
    
    /// <summary>
    /// Baseline metrics (first test result)
    /// </summary>
    public TestResultDto? Baseline { get; init; }
    
    /// <summary>
    /// Latest test result
    /// </summary>
    public TestResultDto? Latest { get; init; }
    
    /// <summary>
    /// Performance trend analysis
    /// </summary>
    public PerformanceTrendDto? Trend { get; init; }
    
    /// <summary>
    /// Performance impact assessment
    /// </summary>
    public PerformanceImpactLevel OverallImpact { get; init; }
    
    /// <summary>
    /// Recommendations based on analysis
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Data Transfer Object for performance trend analysis
/// </summary>
public record PerformanceTrendDto
{
    /// <summary>
    /// Response time trend (improving, degrading, stable)
    /// </summary>
    public string ResponseTimeTrend { get; init; } = "stable";
    
    /// <summary>
    /// Throughput trend (improving, degrading, stable)
    /// </summary>
    public string ThroughputTrend { get; init; } = "stable";
    
    /// <summary>
    /// Error rate trend (improving, degrading, stable)
    /// </summary>
    public string ErrorRateTrend { get; init; } = "stable";
    
    /// <summary>
    /// Average response time change percentage
    /// </summary>
    public double ResponseTimeChangePercent { get; init; }
    
    /// <summary>
    /// Throughput change percentage
    /// </summary>
    public double ThroughputChangePercent { get; init; }
    
    /// <summary>
    /// Error rate change percentage
    /// </summary>
    public double ErrorRateChangePercent { get; init; }
}
