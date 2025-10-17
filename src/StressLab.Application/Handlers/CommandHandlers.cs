using MediatR;
using Microsoft.Extensions.Logging;
using StressLab.Application.Commands;
using StressLab.Application.DTOs;
using StressLab.Application.Queries;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Exceptions;
using StressLab.Core.Interfaces.Repositories;
using StressLab.Core.Interfaces.Services;

namespace StressLab.Application.Handlers;

/// <summary>
/// Handler for executing performance tests
/// </summary>
public class ExecutePerformanceTestCommandHandler : IRequestHandler<ExecutePerformanceTestCommand, TestResult>
{
    private readonly ITestConfigurationRepository _configurationRepository;
    private readonly IPerformanceTestService _performanceTestService;
    private readonly ILogger<ExecutePerformanceTestCommandHandler> _logger;

    public ExecutePerformanceTestCommandHandler(
        ITestConfigurationRepository configurationRepository,
        IPerformanceTestService performanceTestService,
        ILogger<ExecutePerformanceTestCommandHandler> logger)
    {
        _configurationRepository = configurationRepository;
        _performanceTestService = performanceTestService;
        _logger = logger;
    }

    public async Task<TestResult> Handle(ExecutePerformanceTestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting performance test execution for configuration {ConfigurationId}", request.ConfigurationId);

        try
        {
            var configuration = await _configurationRepository.GetByIdAsync(request.ConfigurationId, cancellationToken);
            if (configuration is null)
            {
                throw new TestConfigurationNotFoundException(request.ConfigurationId);
            }

            _logger.LogInformation("Executing test: {TestName} of type {TestType}", configuration.Name, configuration.TestType);

            var result = await _performanceTestService.ExecuteTestAsync(configuration, cancellationToken);

            _logger.LogInformation("Performance test completed successfully. Result ID: {ResultId}", result.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute performance test for configuration {ConfigurationId}", request.ConfigurationId);
            throw;
        }
    }
}

/// <summary>
/// Handler for creating test configurations
/// </summary>
public class CreateTestConfigurationCommandHandler : IRequestHandler<CreateTestConfigurationCommand, TestConfiguration>
{
    private readonly ITestConfigurationRepository _configurationRepository;
    private readonly ILogger<CreateTestConfigurationCommandHandler> _logger;

    public CreateTestConfigurationCommandHandler(
        ITestConfigurationRepository configurationRepository,
        ILogger<CreateTestConfigurationCommandHandler> logger)
    {
        _configurationRepository = configurationRepository;
        _logger = logger;
    }

    public async Task<TestConfiguration> Handle(CreateTestConfigurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new test configuration: {TestName}", request.Configuration.Name);

        try
        {
            var configuration = new TestConfiguration
            {
                Name = request.Configuration.Name,
                Description = request.Configuration.Description,
                TestType = request.Configuration.TestType,
                DurationSeconds = request.Configuration.DurationSeconds,
                ConcurrentUsers = request.Configuration.ConcurrentUsers,
                RampUpSeconds = request.Configuration.RampUpSeconds,
                ApiEndpoint = request.Configuration.ApiEndpoint,
                ApiMethod = request.Configuration.ApiMethod,
                SqlConnectionString = request.Configuration.SqlConnectionString,
                SqlProcedureName = request.Configuration.SqlProcedureName,
                ExpectedResponseTimeMs = request.Configuration.ExpectedResponseTimeMs,
                MaxErrorRatePercent = request.Configuration.MaxErrorRatePercent
            };

            var result = await _configurationRepository.CreateAsync(configuration, cancellationToken);

            _logger.LogInformation("Test configuration created successfully. ID: {ConfigurationId}", result.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test configuration: {TestName}", request.Configuration.Name);
            throw;
        }
    }
}

/// <summary>
/// Handler for updating test configurations
/// </summary>
public class UpdateTestConfigurationCommandHandler : IRequestHandler<UpdateTestConfigurationCommand, TestConfiguration>
{
    private readonly ITestConfigurationRepository _configurationRepository;
    private readonly ILogger<UpdateTestConfigurationCommandHandler> _logger;

    public UpdateTestConfigurationCommandHandler(
        ITestConfigurationRepository configurationRepository,
        ILogger<UpdateTestConfigurationCommandHandler> logger)
    {
        _configurationRepository = configurationRepository;
        _logger = logger;
    }

    public async Task<TestConfiguration> Handle(UpdateTestConfigurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating test configuration: {ConfigurationId}", request.Id);

        try
        {
            var existingConfiguration = await _configurationRepository.GetByIdAsync(request.Id, cancellationToken);
            if (existingConfiguration is null)
            {
                throw new TestConfigurationNotFoundException(request.Id);
            }

            var updatedConfiguration = existingConfiguration with
            {
                Name = request.Configuration.Name,
                Description = request.Configuration.Description,
                TestType = request.Configuration.TestType,
                DurationSeconds = request.Configuration.DurationSeconds,
                ConcurrentUsers = request.Configuration.ConcurrentUsers,
                RampUpSeconds = request.Configuration.RampUpSeconds,
                ApiEndpoint = request.Configuration.ApiEndpoint,
                ApiMethod = request.Configuration.ApiMethod,
                SqlConnectionString = request.Configuration.SqlConnectionString,
                SqlProcedureName = request.Configuration.SqlProcedureName,
                ExpectedResponseTimeMs = request.Configuration.ExpectedResponseTimeMs,
                MaxErrorRatePercent = request.Configuration.MaxErrorRatePercent
            };

            var result = await _configurationRepository.UpdateAsync(updatedConfiguration, cancellationToken);

            _logger.LogInformation("Test configuration updated successfully. ID: {ConfigurationId}", result.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update test configuration: {ConfigurationId}", request.Id);
            throw;
        }
    }
}

/// <summary>
/// Handler for deleting test configurations
/// </summary>
public class DeleteTestConfigurationCommandHandler : IRequestHandler<DeleteTestConfigurationCommand, bool>
{
    private readonly ITestConfigurationRepository _configurationRepository;
    private readonly ILogger<DeleteTestConfigurationCommandHandler> _logger;

    public DeleteTestConfigurationCommandHandler(
        ITestConfigurationRepository configurationRepository,
        ILogger<DeleteTestConfigurationCommandHandler> logger)
    {
        _configurationRepository = configurationRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteTestConfigurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting test configuration: {ConfigurationId}", request.Id);

        try
        {
            var configuration = await _configurationRepository.GetByIdAsync(request.Id, cancellationToken);
            if (configuration is null)
            {
                throw new TestConfigurationNotFoundException(request.Id);
            }

            await _configurationRepository.DeleteAsync(request.Id, cancellationToken);

            _logger.LogInformation("Test configuration deleted successfully. ID: {ConfigurationId}", request.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete test configuration: {ConfigurationId}", request.Id);
            throw;
        }
    }
}

/// <summary>
/// Handler for executing scenarios by name
/// </summary>
public class ExecuteScenarioCommandHandler : IRequestHandler<ExecuteScenarioCommand, TestResult>
{
    private readonly IScenarioExecutionService _scenarioExecutionService;
    private readonly ILogger<ExecuteScenarioCommandHandler> _logger;

    public ExecuteScenarioCommandHandler(
        IScenarioExecutionService scenarioExecutionService,
        ILogger<ExecuteScenarioCommandHandler> logger)
    {
        _scenarioExecutionService = scenarioExecutionService;
        _logger = logger;
    }

    public async Task<TestResult> Handle(ExecuteScenarioCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing scenario: {ScenarioName}", request.ScenarioName);

        try
        {
            var result = await _scenarioExecutionService.ExecuteScenarioByNameAsync(request.ScenarioName, cancellationToken);

            _logger.LogInformation("Scenario executed successfully: {ScenarioName}", request.ScenarioName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scenario: {ScenarioName}", request.ScenarioName);
            throw;
        }
    }
}

/// <summary>
/// Handler for listing available scenarios
/// </summary>
public class ListScenariosCommandHandler : IRequestHandler<ListScenariosCommand, IEnumerable<string>>
{
    private readonly IScenarioConfigurationService _scenarioService;
    private readonly ILogger<ListScenariosCommandHandler> _logger;

    public ListScenariosCommandHandler(
        IScenarioConfigurationService scenarioService,
        ILogger<ListScenariosCommandHandler> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(ListScenariosCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing available scenarios");

        var scenarioNames = _scenarioService.GetScenarioNames().ToList();

        _logger.LogInformation("Found {Count} scenarios", scenarioNames.Count);

        return await Task.FromResult(scenarioNames);
    }
}

/// <summary>
/// Handler for loading scenarios from file
/// </summary>
public class LoadScenariosCommandHandler : IRequestHandler<LoadScenariosCommand, bool>
{
    private readonly IScenarioConfigurationService _scenarioService;
    private readonly ILogger<LoadScenariosCommandHandler> _logger;

    public LoadScenariosCommandHandler(
        IScenarioConfigurationService scenarioService,
        ILogger<LoadScenariosCommandHandler> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    public async Task<bool> Handle(LoadScenariosCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading scenarios from file: {FilePath}", request.ScenariosFilePath);

        try
        {
            await _scenarioService.LoadScenariosAsync(request.ScenariosFilePath, cancellationToken);

            _logger.LogInformation("Scenarios loaded successfully from: {FilePath}", request.ScenariosFilePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scenarios from file: {FilePath}", request.ScenariosFilePath);
            return false;
        }
    }
}