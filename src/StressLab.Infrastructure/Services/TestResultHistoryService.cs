using Microsoft.Extensions.Logging;
using StressLab.Core.Entities;
using StressLab.Core.Interfaces.Repositories;
using StressLab.Core.Interfaces.Services;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for managing test result history and performance analysis
/// </summary>
public class TestResultHistoryService : ITestResultHistoryService
{
    private readonly ITestResultHistoryRepository _historyRepository;
    private readonly ILogger<TestResultHistoryService> _logger;

    public TestResultHistoryService(
        ITestResultHistoryRepository historyRepository,
        ILogger<TestResultHistoryService> logger)
    {
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task LogTestResultAsync(TestResult testResult, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging test result to history: {TestName}", testResult.TestName);

        try
        {
            var history = new TestResultHistory
            {
                Id = Guid.NewGuid(),
                TestName = testResult.TestName,
                ExecutionDate = testResult.StartTime.DateTime,
                DurationSeconds = testResult.DurationSeconds ?? 0,
                TotalRequests = testResult.TotalRequests,
                SuccessfulRequests = testResult.SuccessfulRequests,
                FailedRequests = testResult.FailedRequests,
                ErrorRatePercent = testResult.ErrorRatePercent,
                AverageResponseTimeMs = testResult.AverageResponseTimeMs,
                MinResponseTimeMs = testResult.MinResponseTimeMs,
                MaxResponseTimeMs = testResult.MaxResponseTimeMs,
                P95ResponseTimeMs = testResult.P95ResponseTimeMs,
                P99ResponseTimeMs = testResult.P99ResponseTimeMs,
                RequestsPerSecond = testResult.RequestsPerSecond,
                CpuUsagePercent = testResult.CpuUsagePercent,
                MemoryUsagePercent = testResult.MemoryUsagePercent,
                PerformanceImpact = testResult.PerformanceImpact,
                Status = testResult.Status,
                TestConfigurationId = testResult.TestConfigurationId,
                TestResultId = testResult.Id
            };

            await _historyRepository.CreateAsync(history, cancellationToken);
            
            _logger.LogInformation("Test result logged to history successfully: {TestName}", testResult.TestName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log test result to history: {TestName}", testResult.TestName);
            throw;
        }
    }

    public async Task<PerformanceAnalysis?> AnalyzePerformanceDeviationAsync(TestResult testResult, int sampleSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing performance deviation for test: {TestName}", testResult.TestName);

        try
        {
            var baseline = await _historyRepository.GetBaselineAsync(testResult.TestName, sampleSize, cancellationToken);
            
            if (baseline is null)
            {
                _logger.LogWarning("Insufficient historical data for performance analysis: {TestName}", testResult.TestName);
                return null;
            }

            var analysis = CalculatePerformanceAnalysis(testResult, baseline);
            
            // TODO: Send email notification for significant deviations
            // Consider sending email when:
            // - OverallDeviationScore > 20% (significant performance change)
            // - ResponseTimeDeviationPercent > 30% (response time degraded)
            // - ErrorRateDeviationPercent > 50% (error rate increased significantly)
            // - TrendDirection == TrendDirection.Degrading (performance degrading trend)
            // 
            // Example integration:
            // if (analysis.OverallDeviationScore > 20 || analysis.TrendDirection == TrendDirection.Degrading)
            // {
            //     await _emailNotificationService.SendPerformanceAlertAsync(testResult.TestName, analysis, cancellationToken);
            // }
            
            _logger.LogInformation("Performance analysis completed for test: {TestName} - Overall deviation: {Deviation:F2}%", 
                testResult.TestName, analysis.OverallDeviationScore);
            
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze performance deviation for test: {TestName}", testResult.TestName);
            throw;
        }
    }

    public async Task<PerformanceAnalysis?> GetPerformanceTrendAsync(string testName, int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing performance trend for test: {TestName} over {Days} days", testName, days);

        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            var toDate = DateTime.UtcNow;
            
            var historicalResults = await _historyRepository.GetByTestNameAndDateRangeAsync(testName, fromDate, toDate, cancellationToken);
            var resultsList = historicalResults.ToList();
            
            if (resultsList.Count < 3)
            {
                _logger.LogWarning("Insufficient historical data for trend analysis: {TestName} - {Count} samples", testName, resultsList.Count);
                return null;
            }

            // Use oldest results as baseline, newest as current
            var baseline = resultsList.OrderBy(r => r.ExecutionDate).First();
            var current = resultsList.OrderByDescending(r => r.ExecutionDate).First();
            
            // Convert TestResultHistory to TestResult for analysis
            var currentTestResult = new TestResult
            {
                Id = current.Id,
                TestConfigurationId = current.TestConfigurationId ?? Guid.NewGuid(),
                TestName = current.TestName,
                Description = null,
                Status = current.Status,
                StartTime = current.ExecutionDate,
                EndTime = current.ExecutionDate.AddSeconds(current.DurationSeconds),
                DurationSeconds = current.DurationSeconds,
                TotalRequests = current.TotalRequests,
                SuccessfulRequests = current.SuccessfulRequests,
                FailedRequests = current.FailedRequests,
                ErrorRatePercent = current.ErrorRatePercent,
                AverageResponseTimeMs = current.AverageResponseTimeMs,
                MinResponseTimeMs = current.MinResponseTimeMs,
                MaxResponseTimeMs = current.MaxResponseTimeMs,
                P95ResponseTimeMs = current.P95ResponseTimeMs,
                P99ResponseTimeMs = current.P99ResponseTimeMs,
                RequestsPerSecond = current.RequestsPerSecond,
                CpuUsagePercent = current.CpuUsagePercent,
                MemoryUsagePercent = current.MemoryUsagePercent,
                PerformanceImpact = current.PerformanceImpact,
                MaxErrorRatePercent = 5.0,
                ErrorMessage = null,
                Notes = null
            };
            
            var analysis = CalculatePerformanceAnalysis(currentTestResult, baseline);
            
            // Determine trend direction based on recent results
            var recentResults = resultsList.OrderByDescending(r => r.ExecutionDate).Take(5).ToList();
            analysis = analysis with { TrendDirection = DetermineTrendDirection(recentResults) };
            
            // TODO: Send email notification for trend analysis results
            // Consider sending email when:
            // - TrendDirection == TrendDirection.Degrading (performance degrading over time)
            // - OverallDeviationScore > 15% (moderate performance change over time)
            // - Multiple consecutive degrading results detected
            // 
            // Example integration:
            // if (analysis.TrendDirection == TrendDirection.Degrading && analysis.OverallDeviationScore > 15)
            // {
            //     await _emailNotificationService.SendTrendAlertAsync(testName, analysis, cancellationToken);
            // }
            
            _logger.LogInformation("Performance trend analysis completed for test: {TestName} - Trend: {Trend}", 
                testName, analysis.TrendDirection);
            
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze performance trend for test: {TestName}", testName);
            throw;
        }
    }

    public async Task<TestResultHistory?> GetBaselineAsync(string testName, int sampleSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting baseline for test: {TestName} using {SampleSize} samples", testName, sampleSize);
        
        return await _historyRepository.GetBaselineAsync(testName, sampleSize, cancellationToken);
    }

    public async Task<int> CleanupOldRecordsAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up historical records older than {RetentionDays} days", retentionDays);
        
        return await _historyRepository.CleanupOldRecordsAsync(retentionDays, cancellationToken);
    }

    private PerformanceAnalysis CalculatePerformanceAnalysis(TestResult current, TestResultHistory baseline)
    {
        var responseTimeDeviation = CalculateDeviationPercent(current.AverageResponseTimeMs, baseline.AverageResponseTimeMs);
        var errorRateDeviation = CalculateDeviationPercent(current.ErrorRatePercent, baseline.ErrorRatePercent);
        var throughputDeviation = CalculateDeviationPercent(current.RequestsPerSecond, baseline.RequestsPerSecond);
        
        // Weighted overall deviation score (response time is most important)
        var overallDeviationScore = (responseTimeDeviation * 0.5) + (errorRateDeviation * 0.3) + (throughputDeviation * 0.2);
        
        var recommendations = GenerateRecommendations(responseTimeDeviation, errorRateDeviation, throughputDeviation);
        
        return new PerformanceAnalysis
        {
            TestName = current.TestName,
            AnalysisDate = DateTime.UtcNow,
            BaselineAverageResponseTimeMs = baseline.AverageResponseTimeMs,
            BaselineErrorRatePercent = baseline.ErrorRatePercent,
            BaselineRequestsPerSecond = baseline.RequestsPerSecond,
            CurrentAverageResponseTimeMs = current.AverageResponseTimeMs,
            CurrentErrorRatePercent = current.ErrorRatePercent,
            CurrentRequestsPerSecond = current.RequestsPerSecond,
            ResponseTimeDeviationPercent = responseTimeDeviation,
            ErrorRateDeviationPercent = errorRateDeviation,
            ThroughputDeviationPercent = throughputDeviation,
            OverallDeviationScore = overallDeviationScore,
            TrendDirection = TrendDirection.Stable, // Will be overridden in trend analysis
            SampleSize = 1, // Will be updated based on actual sample size
            ConfidenceLevel = CalculateConfidenceLevel(baseline.AverageResponseTimeMs, current.AverageResponseTimeMs),
            Recommendations = recommendations
        };
    }

    private static double CalculateDeviationPercent(double current, double baseline)
    {
        if (baseline == 0) return 0;
        return ((current - baseline) / baseline) * 100;
    }

    private static TrendDirection DetermineTrendDirection(List<TestResultHistory> recentResults)
    {
        if (recentResults.Count < 3) return TrendDirection.Stable;
        
        var responseTimes = recentResults.Select(r => r.AverageResponseTimeMs).ToList();
        var errorRates = recentResults.Select(r => r.ErrorRatePercent).ToList();
        
        // Simple trend analysis: compare first half vs second half
        var midPoint = recentResults.Count / 2;
        var firstHalfAvgResponseTime = responseTimes.Take(midPoint).Average();
        var secondHalfAvgResponseTime = responseTimes.Skip(midPoint).Average();
        var firstHalfAvgErrorRate = errorRates.Take(midPoint).Average();
        var secondHalfAvgErrorRate = errorRates.Skip(midPoint).Average();
        
        var responseTimeImprovement = firstHalfAvgResponseTime - secondHalfAvgResponseTime;
        var errorRateImprovement = firstHalfAvgErrorRate - secondHalfAvgErrorRate;
        
        // If both metrics improved significantly, trend is improving
        if (responseTimeImprovement > 0.1 * firstHalfAvgResponseTime && errorRateImprovement > 0.1 * firstHalfAvgErrorRate)
            return TrendDirection.Improving;
        
        // If both metrics degraded significantly, trend is degrading
        if (responseTimeImprovement < -0.1 * firstHalfAvgResponseTime && errorRateImprovement < -0.1 * firstHalfAvgErrorRate)
            return TrendDirection.Degrading;
        
        return TrendDirection.Stable;
    }

    private static double CalculateConfidenceLevel(double baseline, double current)
    {
        // Simple confidence calculation based on deviation magnitude
        var deviation = Math.Abs(CalculateDeviationPercent(current, baseline));
        
        if (deviation < 5) return 95.0;
        if (deviation < 10) return 85.0;
        if (deviation < 20) return 75.0;
        if (deviation < 50) return 60.0;
        return 50.0;
    }

    private static List<string> GenerateRecommendations(double responseTimeDeviation, double errorRateDeviation, double throughputDeviation)
    {
        var recommendations = new List<string>();
        
        if (responseTimeDeviation > 20)
        {
            recommendations.Add("Response time has increased significantly. Consider optimizing database queries, caching, or infrastructure scaling.");
            
            // TODO: Send critical performance alert email
            // This is a critical performance degradation that requires immediate attention
            // Example integration:
            // await _emailNotificationService.SendCriticalAlertAsync(
            //     "Response Time Degradation", 
            //     $"Response time increased by {responseTimeDeviation:F1}%", 
            //     recommendations, 
            //     cancellationToken);
        }
        else if (responseTimeDeviation < -20)
        {
            recommendations.Add("Response time has improved significantly. This is a positive trend.");
        }
        
        if (errorRateDeviation > 10)
        {
            recommendations.Add("Error rate has increased. Investigate application logs and system stability.");
            
            // TODO: Send error rate alert email
            // High error rate increase indicates potential system instability
            // Example integration:
            // await _emailNotificationService.SendErrorRateAlertAsync(
            //     $"Error rate increased by {errorRateDeviation:F1}%", 
            //     recommendations, 
            //     cancellationToken);
        }
        else if (errorRateDeviation < -10)
        {
            recommendations.Add("Error rate has decreased. System reliability is improving.");
        }
        
        if (throughputDeviation < -20)
        {
            recommendations.Add("Throughput has decreased. Consider load balancing or infrastructure optimization.");
            
            // TODO: Send throughput degradation alert email
            // Significant throughput decrease may indicate capacity issues
            // Example integration:
            // await _emailNotificationService.SendThroughputAlertAsync(
            //     $"Throughput decreased by {Math.Abs(throughputDeviation):F1}%", 
            //     recommendations, 
            //     cancellationToken);
        }
        else if (throughputDeviation > 20)
        {
            recommendations.Add("Throughput has increased significantly. System capacity is improving.");
        }
        
        if (recommendations.Count == 0)
        {
            recommendations.Add("Performance metrics are within normal ranges. Continue monitoring.");
        }
        
        return recommendations;
    }
}
