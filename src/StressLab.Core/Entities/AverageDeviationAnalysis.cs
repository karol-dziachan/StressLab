using StressLab.Core.Enums;

namespace StressLab.Core.Entities;

/// <summary>
/// Represents the analysis of performance deviation from historical average
/// </summary>
public class AverageDeviationAnalysis
{
    /// <summary>
    /// Name of the test being analyzed
    /// </summary>
    public required string TestName { get; init; }
    
    /// <summary>
    /// Date and time when the analysis was performed
    /// </summary>
    public required DateTime AnalysisDate { get; init; }
    
    /// <summary>
    /// Number of historical samples used for analysis
    /// </summary>
    public required int SampleSize { get; init; }
    
    // Current test metrics
    /// <summary>
    /// Current test average response time in milliseconds
    /// </summary>
    public required double CurrentAverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Current test error rate percentage
    /// </summary>
    public required double CurrentErrorRatePercent { get; init; }
    
    /// <summary>
    /// Current test requests per second
    /// </summary>
    public required double CurrentRequestsPerSecond { get; init; }
    
    /// <summary>
    /// Current test CPU usage percentage
    /// </summary>
    public required double CurrentCpuUsagePercent { get; init; }
    
    /// <summary>
    /// Current test memory usage percentage
    /// </summary>
    public required double CurrentMemoryUsagePercent { get; init; }
    
    // Historical averages
    /// <summary>
    /// Historical average response time in milliseconds
    /// </summary>
    public required double HistoricalAverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Historical average error rate percentage
    /// </summary>
    public required double HistoricalAverageErrorRatePercent { get; init; }
    
    /// <summary>
    /// Historical average requests per second
    /// </summary>
    public required double HistoricalAverageRequestsPerSecond { get; init; }
    
    /// <summary>
    /// Historical average CPU usage percentage
    /// </summary>
    public required double HistoricalAverageCpuUsagePercent { get; init; }
    
    /// <summary>
    /// Historical average memory usage percentage
    /// </summary>
    public required double HistoricalAverageMemoryUsagePercent { get; init; }
    
    // Deviations
    /// <summary>
    /// Response time deviation percentage from historical average
    /// </summary>
    public required double ResponseTimeDeviationPercent { get; init; }
    
    /// <summary>
    /// Error rate deviation percentage from historical average
    /// </summary>
    public required double ErrorRateDeviationPercent { get; init; }
    
    /// <summary>
    /// Throughput deviation percentage from historical average
    /// </summary>
    public required double ThroughputDeviationPercent { get; init; }
    
    /// <summary>
    /// CPU usage deviation percentage from historical average
    /// </summary>
    public required double CpuUsageDeviationPercent { get; init; }
    
    /// <summary>
    /// Memory usage deviation percentage from historical average
    /// </summary>
    public required double MemoryUsageDeviationPercent { get; init; }
    
    /// <summary>
    /// Overall deviation score (weighted average of all deviations)
    /// </summary>
    public required double OverallDeviationScore { get; init; }
    
    /// <summary>
    /// Trend direction based on historical data
    /// </summary>
    public required TrendDirection TrendDirection { get; init; }
    
    /// <summary>
    /// Confidence level of the analysis (0-100)
    /// </summary>
    public required double ConfidenceLevel { get; init; }
    
    /// <summary>
    /// Recommendations based on the analysis
    /// </summary>
    public required List<string> Recommendations { get; init; }
}
