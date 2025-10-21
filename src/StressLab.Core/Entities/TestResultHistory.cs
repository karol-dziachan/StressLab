using StressLab.Core.Enums;

namespace StressLab.Core.Entities;

/// <summary>
/// Represents historical test result data for performance trend analysis
/// </summary>
public record TestResultHistory
{
    /// <summary>
    /// Unique identifier for the history record
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Name of the test (for grouping historical data)
    /// </summary>
    public required string TestName { get; init; }
    
    /// <summary>
    /// Date and time when the test was executed
    /// </summary>
    public required DateTime ExecutionDate { get; init; }
    
    /// <summary>
    /// Test duration in seconds
    /// </summary>
    public required double DurationSeconds { get; init; }
    
    /// <summary>
    /// Total number of requests executed
    /// </summary>
    public required long TotalRequests { get; init; }
    
    /// <summary>
    /// Number of successful requests
    /// </summary>
    public required long SuccessfulRequests { get; init; }
    
    /// <summary>
    /// Number of failed requests
    /// </summary>
    public required long FailedRequests { get; init; }
    
    /// <summary>
    /// Error rate percentage
    /// </summary>
    public required double ErrorRatePercent { get; init; }
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public required double AverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Minimum response time in milliseconds
    /// </summary>
    public required double MinResponseTimeMs { get; init; }
    
    /// <summary>
    /// Maximum response time in milliseconds
    /// </summary>
    public required double MaxResponseTimeMs { get; init; }
    
    /// <summary>
    /// 95th percentile response time in milliseconds
    /// </summary>
    public required double P95ResponseTimeMs { get; init; }
    
    /// <summary>
    /// 99th percentile response time in milliseconds
    /// </summary>
    public required double P99ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Requests per second throughput
    /// </summary>
    public required double RequestsPerSecond { get; init; }
    
    /// <summary>
    /// CPU usage percentage during test
    /// </summary>
    public required double CpuUsagePercent { get; init; }
    
    /// <summary>
    /// Memory usage percentage during test
    /// </summary>
    public required double MemoryUsagePercent { get; init; }
    
    /// <summary>
    /// Performance impact level
    /// </summary>
    public required PerformanceImpactLevel PerformanceImpact { get; init; }
    
    /// <summary>
    /// Test execution status
    /// </summary>
    public required TestStatus Status { get; init; }
    
    /// <summary>
    /// Reference to test configuration (optional)
    /// </summary>
    public Guid? TestConfigurationId { get; init; }
    
    /// <summary>
    /// Reference to original test result (optional)
    /// </summary>
    public Guid? TestResultId { get; init; }
}
