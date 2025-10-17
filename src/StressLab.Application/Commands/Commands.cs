using MediatR;
using StressLab.Application.DTOs;
using StressLab.Core.Entities;
using StressLab.Core.Enums;

namespace StressLab.Application.Commands;

/// <summary>
/// Command to execute a performance test
/// </summary>
public record ExecutePerformanceTestCommand : IRequest<TestResult>
{
    /// <summary>
    /// Test configuration ID to execute
    /// </summary>
    public required Guid ConfigurationId { get; init; }
    
    /// <summary>
    /// Optional cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>
/// Command to create a new test configuration
/// </summary>
public record CreateTestConfigurationCommand : IRequest<TestConfiguration>
{
    /// <summary>
    /// Test configuration data
    /// </summary>
    public required TestConfigurationDto Configuration { get; init; }
}

/// <summary>
/// Command to update an existing test configuration
/// </summary>
public record UpdateTestConfigurationCommand : IRequest<TestConfiguration>
{
    /// <summary>
    /// Configuration ID to update
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// Updated test configuration data
    /// </summary>
    public required TestConfigurationDto Configuration { get; init; }
}

/// <summary>
/// Command to delete a test configuration
/// </summary>
public record DeleteTestConfigurationCommand : IRequest<bool>
{
    /// <summary>
    /// Configuration ID to delete
    /// </summary>
    public required Guid Id { get; init; }
}

/// <summary>
/// Command to execute a scenario by name
/// </summary>
public record ExecuteScenarioCommand : IRequest<TestResult>
{
    /// <summary>
    /// Scenario name to execute
    /// </summary>
    public required string ScenarioName { get; init; }
    
    /// <summary>
    /// Optional cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>
/// Command to list available scenarios
/// </summary>
public record ListScenariosCommand : IRequest<IEnumerable<string>>;

/// <summary>
/// Command to load scenarios from file
/// </summary>
public record LoadScenariosCommand : IRequest<bool>
{
    /// <summary>
    /// Path to scenarios JSON file
    /// </summary>
    public required string ScenariosFilePath { get; init; }
    
    /// <summary>
    /// Optional cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>
/// Supported report formats
/// </summary>
public enum ReportFormat
{
    Html,
    Json,
    Csv
}
