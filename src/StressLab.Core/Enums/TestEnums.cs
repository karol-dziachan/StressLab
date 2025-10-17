namespace StressLab.Core.Enums;

/// <summary>
/// Defines the types of performance tests that can be executed
/// </summary>
public enum TestType
{
    /// <summary>
    /// API performance test
    /// </summary>
    Api = 1,
    
    /// <summary>
    /// SQL procedure performance test
    /// </summary>
    Sql = 2,
    
    /// <summary>
    /// Combined API and SQL test
    /// </summary>
    Combined = 3
}

/// <summary>
/// Defines the status of a test execution
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// Test is pending execution
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Test is currently running
    /// </summary>
    Running = 2,
    
    /// <summary>
    /// Test completed successfully
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Test failed with errors
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Test was cancelled
    /// </summary>
    Cancelled = 5
}

/// <summary>
/// Defines the severity level of performance degradation
/// </summary>
public enum PerformanceImpactLevel
{
    /// <summary>
    /// No significant impact detected
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Minor impact detected
    /// </summary>
    Minor = 1,
    
    /// <summary>
    /// Moderate impact detected
    /// </summary>
    Moderate = 2,
    
    /// <summary>
    /// Major impact detected
    /// </summary>
    Major = 3,
    
    /// <summary>
    /// Critical impact detected
    /// </summary>
    Critical = 4
}

