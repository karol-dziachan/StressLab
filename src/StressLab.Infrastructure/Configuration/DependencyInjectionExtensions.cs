using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FluentValidation;
using Serilog;
using StressLab.Core.Entities;
using StressLab.Core.Interfaces.Repositories;
using StressLab.Core.Interfaces.Services;
using StressLab.Infrastructure.Repositories;
using StressLab.Infrastructure.Services;

namespace StressLab.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds StressLab infrastructure services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddStressLabInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register database configuration
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        
        // Register repositories
        services.AddSingleton<ITestConfigurationRepository, InMemoryTestConfigurationRepository>();
        services.AddSingleton<ITestResultRepository, InMemoryTestResultRepository>();
        services.AddScoped<ITestResultHistoryRepository, SqlServerTestResultHistoryRepository>();
        
        // Register services
        services.AddSingleton<IPerformanceTestService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PerformanceTestService>>();
            var httpClient = provider.GetRequiredService<HttpClient>();
            var systemMetricsService = provider.GetRequiredService<ISystemMetricsService>();
            var httpClientConfigService = provider.GetRequiredService<IHttpClientConfigurationService>();
            var failCriteriaOptions = new FailCriteriaOptions();
            var historyService = provider.GetService<ITestResultHistoryService>();
            
            return new PerformanceTestService(logger, httpClient, systemMetricsService, httpClientConfigService, failCriteriaOptions, historyService!);
        });
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ISystemMetricsService, SystemMetricsService>();
        services.AddSingleton<IScenarioConfigurationService, ScenarioConfigurationService>();
        services.AddSingleton<IScenarioExecutionService, ScenarioExecutionService>();
        services.AddSingleton<IHttpClientConfigurationService, HttpClientConfigurationService>();
        services.AddSingleton<ITestResultHistoryService, TestResultHistoryService>();
        
        // Register configuration options
        services.Configure<ScenarioConfigurationOptions>(options =>
        {
            options.ScenariosFilePath = "scenarios.json";
            options.AutoReload = false;
            options.ReloadInterval = TimeSpan.FromMinutes(5);
        });
        
        // Register HttpClient for API tests
        services.AddHttpClient();
        
        return services;
    }
    
    /// <summary>
    /// Adds StressLab infrastructure services to the service collection (without configuration)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddStressLabInfrastructure(this IServiceCollection services)
    {
        // Register repositories (in-memory only)
        services.AddSingleton<ITestConfigurationRepository, InMemoryTestConfigurationRepository>();
        services.AddSingleton<ITestResultRepository, InMemoryTestResultRepository>();
        services.AddSingleton<ITestResultHistoryRepository, InMemoryTestResultHistoryRepository>();
        
        // Register services
        services.AddSingleton<IPerformanceTestService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PerformanceTestService>>();
            var httpClient = provider.GetRequiredService<HttpClient>();
            var systemMetricsService = provider.GetRequiredService<ISystemMetricsService>();
            var httpClientConfigService = provider.GetRequiredService<IHttpClientConfigurationService>();
            var failCriteriaOptions = new FailCriteriaOptions();
            var historyService = provider.GetService<ITestResultHistoryService>();
            
            return new PerformanceTestService(logger, httpClient, systemMetricsService, httpClientConfigService, failCriteriaOptions, historyService!);
        });
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ISystemMetricsService, SystemMetricsService>();
        services.AddSingleton<IScenarioConfigurationService, ScenarioConfigurationService>();
        services.AddSingleton<IScenarioExecutionService, ScenarioExecutionService>();
        services.AddSingleton<IHttpClientConfigurationService, HttpClientConfigurationService>();
        services.AddSingleton<ITestResultHistoryService, TestResultHistoryService>();
        
        // Register configuration options
        services.Configure<ScenarioConfigurationOptions>(options =>
        {
            options.ScenariosFilePath = "scenarios.json";
            options.AutoReload = false;
            options.ReloadInterval = TimeSpan.FromMinutes(5);
        });
        
        // Register HttpClient for API tests
        services.AddHttpClient();
        
        return services;
    }
    
    /// <summary>
    /// Adds StressLab application services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddStressLabApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(StressLab.Application.Commands.ExecutePerformanceTestCommand).Assembly);
        });
        
        // Register FluentValidation
        // Register validators manually
        services.AddScoped<FluentValidation.IValidator<StressLab.Application.DTOs.TestConfigurationDto>, StressLab.Application.Validators.TestConfigurationDtoValidator>();
        
        return services;
    }
    
    /// <summary>
    /// Configures logging for StressLab
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="logLevel">Minimum log level</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddStressLabLogging(this IServiceCollection services, LogLevel logLevel = LogLevel.Information)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(logLevel);
            
            // Add Serilog if available
            // Configure Serilog
            builder.AddConsole();
            builder.AddDebug();
        });
        
        return services;
    }
}
