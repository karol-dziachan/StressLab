using StressLab.Core.Entities;

namespace StressLab.Core.Entities;

/// <summary>
/// Represents performance analysis results comparing current test with historical baseline
/// </summary>
public record PerformanceAnalysis
{
    /// <summary>
    /// Name of the test being analyzed
    /// </summary>
    public required string TestName { get; init; }
    
    /// <summary>
    /// Date when analysis was performed
    /// </summary>
    public required DateTime AnalysisDate { get; init; }
    
    /// <summary>
    /// Baseline average response time from historical data
    /// </summary>
    public required double BaselineAverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Baseline error rate from historical data
    /// </summary>
    public required double BaselineErrorRatePercent { get; init; }
    
    /// <summary>
    /// Baseline throughput from historical data
    /// </summary>
    public required double BaselineRequestsPerSecond { get; init; }
    
    /// <summary>
    /// Current test average response time
    /// </summary>
    public required double CurrentAverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Current test error rate
    /// </summary>
    public required double CurrentErrorRatePercent { get; init; }
    
    /// <summary>
    /// Current test throughput
    /// </summary>
    public required double CurrentRequestsPerSecond { get; init; }
    
    /// <summary>
    /// Response time deviation percentage (positive = slower, negative = faster)
    /// </summary>
    public required double ResponseTimeDeviationPercent { get; init; }
    
    /// <summary>
    /// Error rate deviation percentage (positive = more errors, negative = fewer errors)
    /// </summary>
    public required double ErrorRateDeviationPercent { get; init; }
    
    /// <summary>
    /// Throughput deviation percentage (positive = higher throughput, negative = lower throughput)
    /// </summary>
    public required double ThroughputDeviationPercent { get; init; }
    
    /// <summary>
    /// Overall deviation score (weighted combination of all metrics)
    /// </summary>
    public required double OverallDeviationScore { get; init; }
    
    /// <summary>
    /// Trend direction based on historical data
    /// </summary>
    public required TrendDirection TrendDirection { get; init; }
    
    /// <summary>
    /// Number of historical samples used for analysis
    /// </summary>
    public required int SampleSize { get; init; }
    
    /// <summary>
    /// Statistical confidence level (0-100)
    /// </summary>
    public required double ConfidenceLevel { get; init; }
    
    /// <summary>
    /// Performance recommendations based on analysis
    /// </summary>
    public required List<string> Recommendations { get; init; }
}

/// <summary>
/// Trend direction for performance analysis
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Performance is stable
    /// </summary>
    Stable = 0,
    
    /// <summary>
    /// Performance is improving
    /// </summary>
    Improving = 1,
    
    /// <summary>
    /// Performance is degrading
    /// </summary>
    Degrading = 2
}
