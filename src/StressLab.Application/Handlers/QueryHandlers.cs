using MediatR;
using Microsoft.Extensions.Logging;
using StressLab.Application.DTOs;
using StressLab.Application.Queries;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Exceptions;
using StressLab.Core.Interfaces.Repositories;

namespace StressLab.Application.Handlers;

/// <summary>
/// Handler for getting all test configurations
/// </summary>
public class GetAllTestConfigurationsQueryHandler : IRequestHandler<GetAllTestConfigurationsQuery, IEnumerable<TestConfiguration>>
{
    private readonly ITestConfigurationRepository _repository;
    private readonly ILogger<GetAllTestConfigurationsQueryHandler> _logger;

    public GetAllTestConfigurationsQueryHandler(
        ITestConfigurationRepository repository,
        ILogger<GetAllTestConfigurationsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<TestConfiguration>> Handle(GetAllTestConfigurationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all test configurations");

        var configurations = await _repository.GetAllAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} test configurations", configurations.Count());

        return configurations;
    }
}

/// <summary>
/// Handler for getting a test configuration by ID
/// </summary>
public class GetTestConfigurationByIdQueryHandler : IRequestHandler<GetTestConfigurationByIdQuery, TestConfiguration?>
{
    private readonly ITestConfigurationRepository _repository;
    private readonly ILogger<GetTestConfigurationByIdQueryHandler> _logger;

    public GetTestConfigurationByIdQueryHandler(
        ITestConfigurationRepository repository,
        ILogger<GetTestConfigurationByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TestConfiguration?> Handle(GetTestConfigurationByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving test configuration: {Id}", request.Id);

        var configuration = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (configuration is null)
        {
            _logger.LogWarning("Test configuration not found: {Id}", request.Id);
        }
        else
        {
            _logger.LogInformation("Test configuration retrieved successfully: {Id}", request.Id);
        }

        return configuration;
    }
}

/// <summary>
/// Handler for getting all test results
/// </summary>
public class GetAllTestResultsQueryHandler : IRequestHandler<GetAllTestResultsQuery, IEnumerable<TestResult>>
{
    private readonly ITestResultRepository _repository;
    private readonly ILogger<GetAllTestResultsQueryHandler> _logger;

    public GetAllTestResultsQueryHandler(
        ITestResultRepository repository,
        ILogger<GetAllTestResultsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<TestResult>> Handle(GetAllTestResultsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all test results");

        var results = await _repository.GetAllAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} test results", results.Count());

        return results;
    }
}

/// <summary>
/// Handler for getting test results by configuration ID
/// </summary>
public class GetTestResultsByConfigurationIdQueryHandler : IRequestHandler<GetTestResultsByConfigurationIdQuery, IEnumerable<TestResult>>
{
    private readonly ITestResultRepository _repository;
    private readonly ILogger<GetTestResultsByConfigurationIdQueryHandler> _logger;

    public GetTestResultsByConfigurationIdQueryHandler(
        ITestResultRepository repository,
        ILogger<GetTestResultsByConfigurationIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<TestResult>> Handle(GetTestResultsByConfigurationIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving test results for configuration: {ConfigurationId}", request.ConfigurationId);

        var results = await _repository.GetByConfigurationIdAsync(request.ConfigurationId, cancellationToken);

        _logger.LogInformation("Retrieved {Count} test results for configuration {ConfigurationId}", 
            results.Count(), request.ConfigurationId);

        return results;
    }
}

/// <summary>
/// Handler for getting a test result by ID
/// </summary>
public class GetTestResultByIdQueryHandler : IRequestHandler<GetTestResultByIdQuery, TestResult?>
{
    private readonly ITestResultRepository _repository;
    private readonly ILogger<GetTestResultByIdQueryHandler> _logger;

    public GetTestResultByIdQueryHandler(
        ITestResultRepository repository,
        ILogger<GetTestResultByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TestResult?> Handle(GetTestResultByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving test result: {Id}", request.Id);

        var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (result is null)
        {
            _logger.LogWarning("Test result not found: {Id}", request.Id);
        }
        else
        {
            _logger.LogInformation("Test result retrieved successfully: {Id}", request.Id);
        }

        return result;
    }
}

/// <summary>
/// Handler for performance analysis queries
/// </summary>
public class GetPerformanceAnalysisQueryHandler : IRequestHandler<GetPerformanceAnalysisQuery, PerformanceAnalysisDto>
{
    private readonly ITestConfigurationRepository _configurationRepository;
    private readonly ITestResultRepository _resultRepository;
    private readonly ILogger<GetPerformanceAnalysisQueryHandler> _logger;

    public GetPerformanceAnalysisQueryHandler(
        ITestConfigurationRepository configurationRepository,
        ITestResultRepository resultRepository,
        ILogger<GetPerformanceAnalysisQueryHandler> logger)
    {
        _configurationRepository = configurationRepository;
        _resultRepository = resultRepository;
        _logger = logger;
    }

    public async Task<PerformanceAnalysisDto> Handle(GetPerformanceAnalysisQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing performance analysis for configuration: {ConfigurationId}", request.ConfigurationId);

        var configuration = await _configurationRepository.GetByIdAsync(request.ConfigurationId, cancellationToken);
        if (configuration is null)
        {
            throw new TestConfigurationNotFoundException(request.ConfigurationId);
        }

        var results = await _resultRepository.GetByConfigurationIdAsync(request.ConfigurationId, cancellationToken);
        var orderedResults = results.OrderBy(r => r.StartTime).ToList();

        if (!orderedResults.Any())
        {
            return new PerformanceAnalysisDto
            {
                ConfigurationId = request.ConfigurationId,
                ConfigurationName = configuration.Name,
                OverallImpact = PerformanceImpactLevel.None,
                Recommendations = new List<string> { "No test results available for analysis" }
            };
        }

        var baseline = orderedResults.First();
        var latest = orderedResults.Last();
        var recentResults = orderedResults.TakeLast(request.RecentResultsCount).ToList();

        var trend = CalculatePerformanceTrend(recentResults);
        var overallImpact = AssessOverallImpact(trend);
        var recommendations = GenerateRecommendations(trend, overallImpact);

        var analysis = new PerformanceAnalysisDto
        {
            ConfigurationId = request.ConfigurationId,
            ConfigurationName = configuration.Name,
            Baseline = MapToDto(baseline),
            Latest = MapToDto(latest),
            Trend = trend,
            OverallImpact = overallImpact,
            Recommendations = recommendations
        };

        _logger.LogInformation("Performance analysis completed for configuration: {ConfigurationId}", request.ConfigurationId);

        return analysis;
    }

    private static PerformanceTrendDto CalculatePerformanceTrend(List<TestResult> results)
    {
        if (results.Count < 2)
        {
            return new PerformanceTrendDto();
        }

        var first = results.First();
        var last = results.Last();

        var responseTimeChange = CalculatePercentageChange(first.AverageResponseTimeMs, last.AverageResponseTimeMs);
        var throughputChange = CalculatePercentageChange(first.RequestsPerSecond, last.RequestsPerSecond);
        var errorRateChange = CalculatePercentageChange(first.ErrorRatePercent, last.ErrorRatePercent);

        return new PerformanceTrendDto
        {
            ResponseTimeTrend = DetermineTrend(responseTimeChange),
            ThroughputTrend = DetermineTrend(throughputChange),
            ErrorRateTrend = DetermineTrend(errorRateChange),
            ResponseTimeChangePercent = responseTimeChange,
            ThroughputChangePercent = throughputChange,
            ErrorRateChangePercent = errorRateChange
        };
    }

    private static double CalculatePercentageChange(double baseline, double current)
    {
        if (baseline == 0) return 0;
        return ((current - baseline) / baseline) * 100;
    }

    private static string DetermineTrend(double changePercent)
    {
        return changePercent switch
        {
            > 10 => "degrading",
            < -10 => "improving",
            _ => "stable"
        };
    }

    private static PerformanceImpactLevel AssessOverallImpact(PerformanceTrendDto trend)
    {
        var degradingCount = 0;
        if (trend.ResponseTimeTrend == "degrading") degradingCount++;
        if (trend.ThroughputTrend == "degrading") degradingCount++;
        if (trend.ErrorRateTrend == "degrading") degradingCount++;

        return degradingCount switch
        {
            0 => PerformanceImpactLevel.None,
            1 => PerformanceImpactLevel.Minor,
            2 => PerformanceImpactLevel.Moderate,
            3 => PerformanceImpactLevel.Major,
            _ => PerformanceImpactLevel.Critical
        };
    }

    private static List<string> GenerateRecommendations(PerformanceTrendDto trend, PerformanceImpactLevel impact)
    {
        var recommendations = new List<string>();

        if (trend.ResponseTimeTrend == "degrading")
        {
            recommendations.Add("Consider optimizing API endpoints or database queries to improve response times");
        }

        if (trend.ThroughputTrend == "degrading")
        {
            recommendations.Add("Review system capacity and consider scaling resources");
        }

        if (trend.ErrorRateTrend == "degrading")
        {
            recommendations.Add("Investigate error patterns and improve error handling");
        }

        if (impact >= PerformanceImpactLevel.Major)
        {
            recommendations.Add("Immediate attention required - performance degradation is significant");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Performance is stable - continue monitoring");
        }

        return recommendations;
    }

    private static TestResultDto MapToDto(TestResult result)
    {
        return new TestResultDto
        {
            Id = result.Id,
            TestConfigurationId = result.TestConfigurationId,
            TestName = result.TestName,
            Status = result.Status,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            DurationSeconds = result.DurationSeconds,
            TotalRequests = result.TotalRequests,
            SuccessfulRequests = result.SuccessfulRequests,
            FailedRequests = result.FailedRequests,
            ErrorRatePercent = result.ErrorRatePercent,
            AverageResponseTimeMs = result.AverageResponseTimeMs,
            MinResponseTimeMs = result.MinResponseTimeMs,
            MaxResponseTimeMs = result.MaxResponseTimeMs,
            P95ResponseTimeMs = result.P95ResponseTimeMs,
            P99ResponseTimeMs = result.P99ResponseTimeMs,
            RequestsPerSecond = result.RequestsPerSecond,
            CpuUsagePercent = result.CpuUsagePercent,
            MemoryUsagePercent = result.MemoryUsagePercent,
            SqlMetrics = result.SqlMetrics is not null ? new SqlMetricsDto
            {
                AverageExecutionTimeMs = result.SqlMetrics.AverageExecutionTimeMs,
                MaxExecutionTimeMs = result.SqlMetrics.MaxExecutionTimeMs,
                ExecutionCount = result.SqlMetrics.ExecutionCount,
                ErrorCount = result.SqlMetrics.ErrorCount,
                DatabaseCpuUsagePercent = result.SqlMetrics.DatabaseCpuUsagePercent,
                DatabaseMemoryUsagePercent = result.SqlMetrics.DatabaseMemoryUsagePercent,
                ActiveConnections = result.SqlMetrics.ActiveConnections,
                LockWaitTimeMs = result.SqlMetrics.LockWaitTimeMs,
                DeadlockCount = result.SqlMetrics.DeadlockCount
            } : null,
            PerformanceImpact = result.PerformanceImpact,
            Notes = result.Notes,
            ReportPath = result.ReportPath,
            CreatedAt = result.CreatedAt
        };
    }
}
