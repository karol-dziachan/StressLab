namespace StressLab.Core.Entities;

/// <summary>
/// Configurable thresholds that determine when a performance test should be marked as Failed.
/// Values are optional; unset values are ignored. Reasonable defaults are applied by services.
/// </summary>
public record FailCriteriaOptions
{
    /// <summary>
    /// Maximum allowed error rate in percent (0-100). If null, test configuration value is used.
    /// </summary>
    public double? MaxErrorRatePercent { get; init; }

    /// <summary>
    /// Maximum allowed average response time in milliseconds. If null, ExpectedResponseTimeMs from configuration is used.
    /// </summary>
    public double? MaxAverageResponseTimeMs { get; init; }

    /// <summary>
    /// Maximum allowed P95 response time in milliseconds. If null, derived as 1.5x MaxAverageResponseTimeMs.
    /// </summary>
    public double? MaxP95ResponseTimeMs { get; init; }

    /// <summary>
    /// Maximum allowed P99 response time in milliseconds. If null, derived as 2x MaxAverageResponseTimeMs.
    /// </summary>
    public double? MaxP99ResponseTimeMs { get; init; }

    /// <summary>
    /// Minimum required throughput in requests per second. If null, not enforced.
    /// </summary>
    public double? MinRequestsPerSecond { get; init; }
}


