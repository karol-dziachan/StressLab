using StressLab.Core.Enums;

namespace StressLab.Core.Entities;

/// <summary>
/// Represents the results of a performance test execution
/// </summary>
public record TestResult
{
    /// <summary>
    /// Unique identifier for the test result
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the test configuration used
    /// </summary>
    public required Guid TestConfigurationId { get; init; }
    
    /// <summary>
    /// Name of the executed test
    /// </summary>
    public required string TestName { get; init; }
    
    /// <summary>
    /// Description of the test
    /// </summary>
    public string? Description { get; init; }
    
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
    public SqlMetrics? SqlMetrics { get; init; }
    
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
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Maximum allowed error rate percentage
    /// </summary>
    public double MaxErrorRatePercent { get; init; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// SQL-specific performance metrics
/// </summary>
public record SqlMetrics
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
    
    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Maximum allowed error rate percentage
    /// </summary>
    public double MaxErrorRatePercent { get; init; }
}
