using FluentValidation;
using StressLab.Application.DTOs;
using StressLab.Core.Enums;

namespace StressLab.Application.Validators;

/// <summary>
/// Validator for TestConfigurationDto
/// </summary>
public class TestConfigurationDtoValidator : AbstractValidator<TestConfigurationDto>
{
    public TestConfigurationDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Test configuration name is required")
            .MaximumLength(200)
            .WithMessage("Test configuration name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 seconds")
            .LessThanOrEqualTo(3600)
            .WithMessage("Duration cannot exceed 3600 seconds (1 hour)");

        RuleFor(x => x.ConcurrentUsers)
            .GreaterThan(0)
            .WithMessage("Concurrent users must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Concurrent users cannot exceed 1000");

        RuleFor(x => x.RampUpSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Ramp-up time cannot be negative")
            .LessThanOrEqualTo(x => x.DurationSeconds)
            .WithMessage("Ramp-up time cannot exceed test duration");

        RuleFor(x => x.ApiEndpoint)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ApiEndpoint))
            .WithMessage("API endpoint must be a valid URL");

        RuleFor(x => x.ApiMethod)
            .Must(BeValidHttpMethod)
            .WithMessage("API method must be a valid HTTP method");

        RuleFor(x => x.SqlConnectionString)
            .NotEmpty()
            .When(x => x.TestType == TestType.Sql || x.TestType == TestType.Combined)
            .WithMessage("SQL connection string is required for SQL tests");

        RuleFor(x => x.SqlProcedureName)
            .NotEmpty()
            .When(x => x.TestType == TestType.Sql || x.TestType == TestType.Combined)
            .WithMessage("SQL procedure name is required for SQL tests");

        RuleFor(x => x.ExpectedResponseTimeMs)
            .GreaterThan(0)
            .WithMessage("Expected response time must be greater than 0");

        RuleFor(x => x.MaxErrorRatePercent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max error rate cannot be negative")
            .LessThanOrEqualTo(100)
            .WithMessage("Max error rate cannot exceed 100%");
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidHttpMethod(string method)
    {
        var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
        return validMethods.Contains(method.ToUpperInvariant());
    }
}
