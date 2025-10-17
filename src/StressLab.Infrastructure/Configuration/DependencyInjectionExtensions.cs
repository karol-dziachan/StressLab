using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Serilog;
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
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddStressLabInfrastructure(this IServiceCollection services)
    {
        // Register repositories
        services.AddSingleton<ITestConfigurationRepository, InMemoryTestConfigurationRepository>();
        services.AddSingleton<ITestResultRepository, InMemoryTestResultRepository>();
        
        // Register services
        services.AddSingleton<IPerformanceTestService, PerformanceTestService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<ISystemMetricsService, SystemMetricsService>();
        services.AddSingleton<IScenarioConfigurationService, ScenarioConfigurationService>();
        services.AddSingleton<IScenarioExecutionService, ScenarioExecutionService>();
        services.AddSingleton<IHttpClientConfigurationService, HttpClientConfigurationService>();
        
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
