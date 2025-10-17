using StressLab.Core.Enums;

namespace StressLab.Core.Entities;

/// <summary>
/// Represents a performance test configuration
/// </summary>
public record TestConfiguration
{
    /// <summary>
    /// Unique identifier for the test configuration
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
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
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
}
