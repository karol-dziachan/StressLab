using StressLab.Core.Enums;

namespace StressLab.Core.Entities;

/// <summary>
/// Represents a test scenario configuration
/// </summary>
public record TestScenario
{
    /// <summary>
    /// Unique identifier for the scenario
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Name of the scenario
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description of the scenario
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Steps to execute in this scenario
    /// </summary>
    public required List<TestStep> Steps { get; init; }
    
    /// <summary>
    /// How steps should be executed
    /// </summary>
    public StepExecutionMode ExecutionMode { get; init; } = StepExecutionMode.Parallel;
    
    /// <summary>
    /// Load simulation configuration
    /// </summary>
    public required LoadSimulation LoadSimulation { get; init; }
    
    /// <summary>
    /// Scenario-specific settings
    /// </summary>
    public Dictionary<string, object>? Settings { get; init; }
    
    /// <summary>
    /// Duration of the test in seconds
    /// </summary>
    public int DurationSeconds { get; init; } = 60;
    
    /// <summary>
    /// Number of concurrent users
    /// </summary>
    public int ConcurrentUsers { get; init; } = 10;
    
    /// <summary>
    /// Ramp-up time in seconds
    /// </summary>
    public int RampUpSeconds { get; init; } = 10;
    
    /// <summary>
    /// Maximum allowed error rate percentage
    /// </summary>
    public double MaxErrorRatePercent { get; init; } = 5.0;
    
    /// <summary>
    /// Expected response time in milliseconds
    /// </summary>
    public int ExpectedResponseTimeMs { get; init; } = 1000;
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a single test step
/// </summary>
public record TestStep
{
    /// <summary>
    /// Unique identifier for the step
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Name of the step
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description of what this step does
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Type of step to execute
    /// </summary>
    public required StepType Type { get; init; }
    
    /// <summary>
    /// Configuration specific to the step type
    /// </summary>
    public required Dictionary<string, object> Configuration { get; init; }
    
    /// <summary>
    /// API endpoint for API steps
    /// </summary>
    public string? ApiEndpoint { get; init; }
    
    /// <summary>
    /// HTTP method for API steps
    /// </summary>
    public string? ApiMethod { get; init; }
    
    /// <summary>
    /// SQL connection string for SQL steps
    /// </summary>
    public string? SqlConnectionString { get; init; }
    
    /// <summary>
    /// SQL procedure name for SQL steps
    /// </summary>
    public string? SqlProcedureName { get; init; }
    
    /// <summary>
    /// Whether this step should be combined with the previous one
    /// </summary>
    public bool IsCombinedWithPrevious { get; init; } = false;
    
    /// <summary>
    /// Weight of this step in the scenario (for load distribution)
    /// </summary>
    public int Weight { get; init; } = 1;
    
    /// <summary>
    /// Whether this step should be executed
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Step-specific settings
    /// </summary>
    public Dictionary<string, object>? Settings { get; init; }
}

/// <summary>
/// Represents load simulation configuration
/// </summary>
public record LoadSimulation
{
    /// <summary>
    /// Type of load simulation
    /// </summary>
    public required LoadSimulationType Type { get; init; }
    
    /// <summary>
    /// Rate of requests per second
    /// </summary>
    public int Rate { get; init; } = 10;
    
    /// <summary>
    /// Duration of the test in seconds
    /// </summary>
    public int DurationSeconds { get; init; } = 60;
    
    /// <summary>
    /// Ramp-up time in seconds
    /// </summary>
    public int RampUpSeconds { get; init; } = 10;
    
    /// <summary>
    /// Maximum number of concurrent users
    /// </summary>
    public int MaxConcurrentUsers { get; init; } = 100;
    
    /// <summary>
    /// Additional simulation parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Types of test steps
/// </summary>
public enum StepType
{
    /// <summary>
    /// HTTP API call
    /// </summary>
    HttpApi = 1,
    
    /// <summary>
    /// SQL stored procedure execution
    /// </summary>
    SqlProcedure = 2,
    
    /// <summary>
    /// SQL query execution
    /// </summary>
    SqlQuery = 3,
    
    /// <summary>
    /// Wait/delay step
    /// </summary>
    Wait = 4,
    
    /// <summary>
    /// Custom script execution
    /// </summary>
    CustomScript = 5,
    
    /// <summary>
    /// File operation
    /// </summary>
    FileOperation = 6,
    
    /// <summary>
    /// Database connection test
    /// </summary>
    DatabaseConnection = 7
}

/// <summary>
/// How steps should be executed in a scenario
/// </summary>
public enum StepExecutionMode
{
    /// <summary>
    /// Execute all steps in parallel
    /// </summary>
    Parallel = 1,
    
    /// <summary>
    /// Execute steps sequentially
    /// </summary>
    Sequential = 2,
    
    /// <summary>
    /// Execute steps in groups (parallel within groups, sequential between groups)
    /// </summary>
    Grouped = 3,
    
    /// <summary>
    /// Execute steps based on their weights (weighted distribution)
    /// </summary>
    Weighted = 4
}

/// <summary>
/// Types of load simulation
/// </summary>
public enum LoadSimulationType
{
    /// <summary>
    /// Constant rate injection
    /// </summary>
    ConstantRate = 1,
    
    /// <summary>
    /// Ramp-up injection
    /// </summary>
    RampUp = 2,
    
    /// <summary>
    /// Spike injection
    /// </summary>
    Spike = 3,
    
    /// <summary>
    /// Stress injection
    /// </summary>
    Stress = 4,
    
    /// <summary>
    /// Soak injection
    /// </summary>
    Soak = 5
}
