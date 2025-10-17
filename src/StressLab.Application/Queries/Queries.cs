using MediatR;
using StressLab.Application.DTOs;
using StressLab.Core.Entities;

namespace StressLab.Application.Queries;

/// <summary>
/// Query to get all test configurations
/// </summary>
public record GetAllTestConfigurationsQuery : IRequest<IEnumerable<TestConfiguration>>;

/// <summary>
/// Query to get a test configuration by ID
/// </summary>
public record GetTestConfigurationByIdQuery : IRequest<TestConfiguration?>
{
    /// <summary>
    /// Configuration ID
    /// </summary>
    public required Guid Id { get; init; }
}

/// <summary>
/// Query to get all test results
/// </summary>
public record GetAllTestResultsQuery : IRequest<IEnumerable<TestResult>>;

/// <summary>
/// Query to get test results by configuration ID
/// </summary>
public record GetTestResultsByConfigurationIdQuery : IRequest<IEnumerable<TestResult>>
{
    /// <summary>
    /// Configuration ID
    /// </summary>
    public required Guid ConfigurationId { get; init; }
}

/// <summary>
/// Query to get a test result by ID
/// </summary>
public record GetTestResultByIdQuery : IRequest<TestResult?>
{
    /// <summary>
    /// Result ID
    /// </summary>
    public required Guid Id { get; init; }
}

/// <summary>
/// Query to get test results with performance analysis
/// </summary>
public record GetPerformanceAnalysisQuery : IRequest<PerformanceAnalysisDto>
{
    /// <summary>
    /// Configuration ID to analyze
    /// </summary>
    public required Guid ConfigurationId { get; init; }
    
    /// <summary>
    /// Number of recent results to include in analysis
    /// </summary>
    public int RecentResultsCount { get; init; } = 10;
}
