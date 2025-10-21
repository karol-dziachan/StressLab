using Microsoft.Extensions.Logging;
using StressLab.Core.Entities;
using StressLab.Core.Interfaces.Services;
using System.Text.Json;
using System.Text;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for generating performance test reports
/// </summary>
public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly ITestResultHistoryService? _historyService;

    public ReportService(ILogger<ReportService> logger)
    {
        _logger = logger;
        _historyService = null;
    }

    public ReportService(ILogger<ReportService> logger, ITestResultHistoryService historyService)
    {
        _logger = logger;
        _historyService = historyService;
    }

    public async Task<string> GenerateHtmlReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating HTML report for test result: {ResultId}", result.Id);

        try
        {
            var htmlContent = await GenerateHtmlContent(result);
            var fileName = $"test_report_{result.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
            var fullPath = Path.Combine(outputPath, fileName);

            await File.WriteAllTextAsync(fullPath, htmlContent, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("HTML report generated successfully: {FilePath}", fullPath);

            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML report for test result: {ResultId}", result.Id);
            throw;
        }
    }

    public async Task<string> GenerateCombinedHtmlReportAsync(IEnumerable<TestResult> results, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating combined HTML report for {ResultCount} test results", results.Count());

        try
        {
            var htmlContent = GenerateCombinedHtmlContent(results);
            var fileName = $"combined_test_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
            var fullPath = Path.Combine(outputPath, fileName);

            await File.WriteAllTextAsync(fullPath, htmlContent, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("Combined HTML report generated successfully: {FilePath}", fullPath);

            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate combined HTML report");
            throw;
        }
    }

    public async Task<string> GenerateJsonReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating JSON report for test result: {ResultId}", result.Id);

        try
        {
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var fileName = $"test_report_{result.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var fullPath = Path.Combine(outputPath, fileName);

            await File.WriteAllTextAsync(fullPath, jsonContent, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("JSON report generated successfully: {FilePath}", fullPath);

            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JSON report for test result: {ResultId}", result.Id);
            throw;
        }
    }

    public async Task<string> GenerateCsvReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating CSV report for test result: {ResultId}", result.Id);

        try
        {
            var csvContent = GenerateCsvContent(result);
            var fileName = $"test_report_{result.Id:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            var fullPath = Path.Combine(outputPath, fileName);

            await File.WriteAllTextAsync(fullPath, csvContent, Encoding.UTF8, cancellationToken);

            _logger.LogInformation("CSV report generated successfully: {FilePath}", fullPath);

            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CSV report for test result: {ResultId}", result.Id);
            throw;
        }
    }

    private async Task<string> GenerateHtmlContent(TestResult result)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("    <title>Performance Test Report - " + result.TestName + "</title>");
        html.AppendLine("    <style>");
        html.AppendLine(GetCssStyles());
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        // Header
        html.AppendLine("    <div class=\"header\">");
        html.AppendLine("        <h1>Performance Test Report</h1>");
        html.AppendLine("        <div class=\"test-info\">");
        html.AppendLine($"            <h2>{result.TestName}</h2>");
        html.AppendLine($"            <p class=\"test-id\">Test ID: {result.Id}</p>");
        html.AppendLine($"            <p class=\"test-date\">Executed: {result.StartTime:yyyy-MM-dd HH:mm:ss} UTC</p>");
        html.AppendLine($"            <p class=\"test-status status-{result.Status.ToString().ToLower()}\">Status: {result.Status}</p>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        
        // Summary Cards
        html.AppendLine("    <div class=\"summary-cards\">");
        html.AppendLine("        <div class=\"card\">");
        html.AppendLine("            <h3>Total Requests</h3>");
        html.AppendLine($"            <div class=\"metric-value\">{result.TotalRequests:N0}</div>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"card\">");
        html.AppendLine("            <h3>Success Rate</h3>");
        html.AppendLine($"            <div class=\"metric-value\">{(100 - result.ErrorRatePercent):F2}%</div>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"card\">");
        html.AppendLine("            <h3>Avg Response Time</h3>");
        html.AppendLine($"            <div class=\"metric-value\">{result.AverageResponseTimeMs:F2} ms</div>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"card\">");
        html.AppendLine("            <h3>Throughput</h3>");
        html.AppendLine($"            <div class=\"metric-value\">{result.RequestsPerSecond:F2} req/s</div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        
        // Detailed Metrics
        html.AppendLine("    <div class=\"detailed-metrics\">");
        html.AppendLine("        <h2>Detailed Metrics</h2>");
        html.AppendLine("        <div class=\"metrics-grid\">");
        
        // Response Time Metrics
        html.AppendLine("            <div class=\"metric-section\">");
        html.AppendLine("                <h3>Response Time Metrics</h3>");
        html.AppendLine("                <table>");
        html.AppendLine("                    <tr><td>Average</td><td>" + result.AverageResponseTimeMs.ToString("F2") + " ms</td></tr>");
        html.AppendLine("                    <tr><td>Minimum</td><td>" + result.MinResponseTimeMs.ToString("F2") + " ms</td></tr>");
        html.AppendLine("                    <tr><td>Maximum</td><td>" + result.MaxResponseTimeMs.ToString("F2") + " ms</td></tr>");
        html.AppendLine("                    <tr><td>95th Percentile</td><td>" + result.P95ResponseTimeMs.ToString("F2") + " ms</td></tr>");
        html.AppendLine("                    <tr><td>99th Percentile</td><td>" + result.P99ResponseTimeMs.ToString("F2") + " ms</td></tr>");
        html.AppendLine("                </table>");
        html.AppendLine("            </div>");
        
        // System Metrics
        html.AppendLine("            <div class=\"metric-section\">");
        html.AppendLine("                <h3>System Metrics</h3>");
        html.AppendLine("                <table>");
        html.AppendLine("                    <tr><td>CPU Usage</td><td>" + result.CpuUsagePercent.ToString("F2") + "%</td></tr>");
        html.AppendLine("                    <tr><td>Memory Usage</td><td>" + result.MemoryUsagePercent.ToString("F2") + "%</td></tr>");
        html.AppendLine("                    <tr><td>Test Duration</td><td>" + result.DurationSeconds?.ToString("F2") + " seconds</td></tr>");
        html.AppendLine("                </table>");
        html.AppendLine("            </div>");
        
        // SQL Metrics (if available)
        if (result.SqlMetrics is not null)
        {
            html.AppendLine("            <div class=\"metric-section\">");
            html.AppendLine("                <h3>SQL Metrics</h3>");
            html.AppendLine("                <table>");
            html.AppendLine("                    <tr><td>Execution Count</td><td>" + result.SqlMetrics.ExecutionCount.ToString("N0") + "</td></tr>");
            html.AppendLine("                    <tr><td>Avg Execution Time</td><td>" + result.SqlMetrics.AverageExecutionTimeMs.ToString("F2") + " ms</td></tr>");
            html.AppendLine("                    <tr><td>Max Execution Time</td><td>" + result.SqlMetrics.MaxExecutionTimeMs.ToString("F2") + " ms</td></tr>");
            html.AppendLine("                    <tr><td>Error Count</td><td>" + result.SqlMetrics.ErrorCount.ToString("N0") + "</td></tr>");
            html.AppendLine("                    <tr><td>Active Connections</td><td>" + result.SqlMetrics.ActiveConnections + "</td></tr>");
            html.AppendLine("                    <tr><td>Deadlock Count</td><td>" + result.SqlMetrics.DeadlockCount + "</td></tr>");
            html.AppendLine("                </table>");
            html.AppendLine("            </div>");
        }
        
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        
        // Performance Impact Assessment
        html.AppendLine("    <div class=\"performance-impact\">");
        html.AppendLine("        <h2>Performance Impact Assessment</h2>");
        html.AppendLine($"        <div class=\"impact-level impact-{result.PerformanceImpact.ToString().ToLower()}\">");
        html.AppendLine($"            <h3>{result.PerformanceImpact}</h3>");
        html.AppendLine("        </div>");
        
        // Historical Analysis (if available)
        if (_historyService is not null)
        {
            try
            {
                var analysis = await _historyService.AnalyzePerformanceDeviationAsync(result, cancellationToken: CancellationToken.None);
                if (analysis is not null)
                {
                    html.AppendLine("        <div class=\"historical-analysis\">");
                    html.AppendLine("            <h3>📊 Historical Performance Analysis</h3>");
                    html.AppendLine($"            <div class=\"analysis-summary\">");
                    html.AppendLine($"                <div class=\"analysis-metric\">");
                    html.AppendLine($"                    <span class=\"metric-label\">Overall Deviation:</span>");
                    html.AppendLine($"                    <span class=\"metric-value {(analysis.OverallDeviationScore > 0 ? "degraded" : "improved")}\">{analysis.OverallDeviationScore:F1}%</span>");
                    html.AppendLine($"                </div>");
                    html.AppendLine($"                <div class=\"analysis-metric\">");
                    html.AppendLine($"                    <span class=\"metric-label\">Response Time Change:</span>");
                    html.AppendLine($"                    <span class=\"metric-value {(analysis.ResponseTimeDeviationPercent > 0 ? "degraded" : "improved")}\">{analysis.ResponseTimeDeviationPercent:F1}%</span>");
                    html.AppendLine($"                </div>");
                    html.AppendLine($"                <div class=\"analysis-metric\">");
                    html.AppendLine($"                    <span class=\"metric-label\">Error Rate Change:</span>");
                    html.AppendLine($"                    <span class=\"metric-value {(analysis.ErrorRateDeviationPercent > 0 ? "degraded" : "improved")}\">{analysis.ErrorRateDeviationPercent:F1}%</span>");
                    html.AppendLine($"                </div>");
                    html.AppendLine($"                <div class=\"analysis-metric\">");
                    html.AppendLine($"                    <span class=\"metric-label\">Throughput Change:</span>");
                    html.AppendLine($"                    <span class=\"metric-value {(analysis.ThroughputDeviationPercent > 0 ? "improved" : "degraded")}\">{analysis.ThroughputDeviationPercent:F1}%</span>");
                    html.AppendLine($"                </div>");
                    html.AppendLine($"                <div class=\"analysis-metric\">");
                    html.AppendLine($"                    <span class=\"metric-label\">Trend:</span>");
                    html.AppendLine($"                    <span class=\"metric-value trend-{analysis.TrendDirection.ToString().ToLower()}\">{analysis.TrendDirection}</span>");
                    html.AppendLine($"                </div>");
                    html.AppendLine($"                <div class=\"analysis-metric\">");
                    html.AppendLine($"                    <span class=\"metric-label\">Confidence:</span>");
                    html.AppendLine($"                    <span class=\"metric-value\">{analysis.ConfidenceLevel:F1}%</span>");
                    html.AppendLine($"                </div>");
                    html.AppendLine($"            </div>");
                    
                    if (analysis.Recommendations.Any())
                    {
                        html.AppendLine("            <div class=\"recommendations\">");
                        html.AppendLine("                <h4>💡 Recommendations</h4>");
                        html.AppendLine("                <ul>");
                        foreach (var recommendation in analysis.Recommendations)
                        {
                            html.AppendLine($"                    <li>{recommendation}</li>");
                        }
                        html.AppendLine("                </ul>");
                        html.AppendLine("            </div>");
                    }
                    
                    html.AppendLine("        </div>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate historical analysis for report");
            }
        }
        
        if (!string.IsNullOrEmpty(result.Notes))
        {
            html.AppendLine("        <div class=\"notes\">");
            html.AppendLine("            <h4>Notes</h4>");
            html.AppendLine($"            <p>{result.Notes}</p>");
            html.AppendLine("        </div>");
        }
        html.AppendLine("    </div>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GetCssStyles()
    {
        return @"
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
            color: #333;
        }
        
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 10px;
            margin-bottom: 30px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        
        .header h1 {
            margin: 0 0 20px 0;
            font-size: 2.5em;
        }
        
        .test-info h2 {
            margin: 0 0 10px 0;
            font-size: 1.8em;
        }
        
        .test-id, .test-date {
            margin: 5px 0;
            opacity: 0.9;
        }
        
        .test-status {
            display: inline-block;
            padding: 5px 15px;
            border-radius: 20px;
            font-weight: bold;
            margin-top: 10px;
        }
        
        .status-completed { background-color: #4CAF50; }
        .status-failed { background-color: #f44336; }
        .status-running { background-color: #ff9800; }
        .status-pending { background-color: #9e9e9e; }
        
        .summary-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }
        
        .card {
            background: white;
            padding: 25px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            text-align: center;
        }
        
        .card h3 {
            margin: 0 0 15px 0;
            color: #666;
            font-size: 1.1em;
        }
        
        .metric-value {
            font-size: 2.5em;
            font-weight: bold;
            color: #667eea;
        }
        
        .detailed-metrics {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }
        
        .detailed-metrics h2 {
            margin: 0 0 25px 0;
            color: #333;
        }
        
        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 25px;
        }
        
        .metric-section h3 {
            margin: 0 0 15px 0;
            color: #667eea;
            border-bottom: 2px solid #667eea;
            padding-bottom: 5px;
        }
        
        table {
            width: 100%;
            border-collapse: collapse;
        }
        
        table td {
            padding: 8px 12px;
            border-bottom: 1px solid #eee;
        }
        
        table td:first-child {
            font-weight: 500;
            color: #666;
        }
        
        table td:last-child {
            text-align: right;
            font-weight: bold;
            color: #333;
        }
        
        .performance-impact {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        
        .performance-impact h2 {
            margin: 0 0 20px 0;
            color: #333;
        }
        
        .impact-level {
            display: inline-block;
            padding: 15px 25px;
            border-radius: 10px;
            font-size: 1.2em;
            font-weight: bold;
            margin-bottom: 20px;
        }
        
        .impact-none { background-color: #e8f5e8; color: #2e7d32; }
        .impact-minor { background-color: #fff3e0; color: #f57c00; }
        .impact-moderate { background-color: #fce4ec; color: #c2185b; }
        .impact-major { background-color: #ffebee; color: #d32f2f; }
        .impact-critical { background-color: #f3e5f5; color: #7b1fa2; }
        
        .notes {
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid #667eea;
        }
        
        .notes h4 {
            margin: 0 0 10px 0;
            color: #667eea;
        }
        
        .notes p {
            margin: 0;
            line-height: 1.6;
        }
        
        .historical-analysis {
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid #667eea;
            margin-top: 20px;
        }
        
        .historical-analysis h3 {
            margin: 0 0 15px 0;
            color: #667eea;
        }
        
        .analysis-summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-bottom: 20px;
        }
        
        .analysis-metric {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px;
            background: white;
            border-radius: 6px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        }
        
        .analysis-metric .metric-label {
            font-weight: 500;
            color: #666;
        }
        
        .analysis-metric .metric-value {
            font-weight: bold;
            padding: 4px 8px;
            border-radius: 4px;
        }
        
        .metric-value.improved {
            background-color: #e8f5e8;
            color: #2e7d32;
        }
        
        .metric-value.degraded {
            background-color: #ffebee;
            color: #d32f2f;
        }
        
        .trend-improving {
            background-color: #e8f5e8;
            color: #2e7d32;
        }
        
        .trend-degrading {
            background-color: #ffebee;
            color: #d32f2f;
        }
        
        .trend-stable {
            background-color: #e3f2fd;
            color: #1976d2;
        }
        
        .recommendations {
            background-color: #fff3e0;
            padding: 15px;
            border-radius: 6px;
            border-left: 4px solid #ff9800;
        }
        
        .recommendations h4 {
            margin: 0 0 10px 0;
            color: #f57c00;
        }
        
        .recommendations ul {
            margin: 0;
            padding-left: 20px;
        }
        
        .recommendations li {
            margin-bottom: 5px;
            line-height: 1.5;
        }
        ";
    }

    private string GenerateCsvContent(TestResult result)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Metric,Value");
        
        // Basic metrics
        csv.AppendLine($"Test ID,{result.Id}");
        csv.AppendLine($"Test Name,{result.TestName}");
        csv.AppendLine($"Status,{result.Status}");
        csv.AppendLine($"Start Time,{result.StartTime:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"End Time,{result.EndTime:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Duration (seconds),{result.DurationSeconds}");
        csv.AppendLine($"Total Requests,{result.TotalRequests}");
        csv.AppendLine($"Successful Requests,{result.SuccessfulRequests}");
        csv.AppendLine($"Failed Requests,{result.FailedRequests}");
        csv.AppendLine($"Error Rate (%),{result.ErrorRatePercent}");
        csv.AppendLine($"Average Response Time (ms),{result.AverageResponseTimeMs}");
        csv.AppendLine($"Min Response Time (ms),{result.MinResponseTimeMs}");
        csv.AppendLine($"Max Response Time (ms),{result.MaxResponseTimeMs}");
        csv.AppendLine($"95th Percentile (ms),{result.P95ResponseTimeMs}");
        csv.AppendLine($"99th Percentile (ms),{result.P99ResponseTimeMs}");
        csv.AppendLine($"Requests Per Second,{result.RequestsPerSecond}");
        csv.AppendLine($"CPU Usage (%),{result.CpuUsagePercent}");
        csv.AppendLine($"Memory Usage (%),{result.MemoryUsagePercent}");
        csv.AppendLine($"Performance Impact,{result.PerformanceImpact}");
        
        // SQL metrics (if available)
        if (result.SqlMetrics is not null)
        {
            csv.AppendLine($"SQL Execution Count,{result.SqlMetrics.ExecutionCount}");
            csv.AppendLine($"SQL Avg Execution Time (ms),{result.SqlMetrics.AverageExecutionTimeMs}");
            csv.AppendLine($"SQL Max Execution Time (ms),{result.SqlMetrics.MaxExecutionTimeMs}");
            csv.AppendLine($"SQL Error Count,{result.SqlMetrics.ErrorCount}");
            csv.AppendLine($"SQL Active Connections,{result.SqlMetrics.ActiveConnections}");
            csv.AppendLine($"SQL Deadlock Count,{result.SqlMetrics.DeadlockCount}");
        }
        
        return csv.ToString();
    }

    private string GenerateCombinedHtmlContent(IEnumerable<TestResult> results)
    {
        var resultsList = results.ToList();
        var totalRequests = resultsList.Sum(r => r.TotalRequests);
        var totalSuccessfulRequests = resultsList.Sum(r => r.SuccessfulRequests);
        var totalFailedRequests = resultsList.Sum(r => r.FailedRequests);
        var averageResponseTimeMs = resultsList.Any() ? resultsList.Average(r => r.AverageResponseTimeMs) : 0;
        var averageRequestsPerSecond = resultsList.Any() ? resultsList.Average(r => r.RequestsPerSecond) : 0;
        var averageCpuUsage = resultsList.Any() ? resultsList.Average(r => r.CpuUsagePercent) : 0;
        var averageMemoryUsage = resultsList.Any() ? resultsList.Average(r => r.MemoryUsagePercent) : 0;
        var maxPerformanceImpact = resultsList.Any() ? resultsList.Max(r => r.PerformanceImpact) : Core.Enums.PerformanceImpactLevel.Minor;
        var overallErrorRate = totalRequests > 0 ? (double)totalFailedRequests / totalRequests * 100 : 0;
        var successfulTests = resultsList.Count(r => r.Status == Core.Enums.TestStatus.Completed && r.ErrorRatePercent <= r.MaxErrorRatePercent);
        var failedTests = resultsList.Count - successfulTests;

        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("    <title>Combined Performance Test Report</title>");
        html.AppendLine("    <style>");
        html.AppendLine(GetModernCssStyles());
        html.AppendLine("    </style>");
        html.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        // Header
        html.AppendLine("    <div class=\"header\">");
        html.AppendLine("        <div class=\"header-content\">");
        html.AppendLine("            <h1>🚀 Combined Performance Test Report</h1>");
        html.AppendLine("            <div class=\"header-stats\">");
        html.AppendLine($"                <div class=\"header-stat\">");
        html.AppendLine($"                    <span class=\"stat-number\">{resultsList.Count}</span>");
        html.AppendLine($"                    <span class=\"stat-label\">Total Tests</span>");
        html.AppendLine($"                </div>");
        html.AppendLine($"                <div class=\"header-stat\">");
        html.AppendLine($"                    <span class=\"stat-number\">{successfulTests}</span>");
        html.AppendLine($"                    <span class=\"stat-label\">Successful</span>");
        html.AppendLine($"                </div>");
        html.AppendLine($"                <div class=\"header-stat\">");
        html.AppendLine($"                    <span class=\"stat-number\">{failedTests}</span>");
        html.AppendLine($"                    <span class=\"stat-label\">Failed</span>");
        html.AppendLine($"                </div>");
        html.AppendLine($"                <div class=\"header-stat\">");
        html.AppendLine($"                    <span class=\"stat-number\">{totalRequests:N0}</span>");
        html.AppendLine($"                    <span class=\"stat-label\">Total Requests</span>");
        html.AppendLine($"                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <p class=\"report-date\">Generated on " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC</p>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        
        // Individual Test Results FIRST
        html.AppendLine("    <div class=\"tests-section\">");
        html.AppendLine("        <h2>🧪 Individual Test Results</h2>");
        html.AppendLine("        <div class=\"tests-grid\">");
        
        foreach (var result in resultsList)
        {
            var statusClass = result.Status == Core.Enums.TestStatus.Completed && result.ErrorRatePercent <= result.MaxErrorRatePercent ? "success" : "failed";
            var statusIcon = statusClass == "success" ? "✅" : "❌";
            
            html.AppendLine($"            <div class=\"test-card {statusClass}\">");
            html.AppendLine($"                <div class=\"test-header\">");
            html.AppendLine($"                    <h3>{statusIcon} {result.TestName}</h3>");
            html.AppendLine($"                    <span class=\"test-status\">{result.Status}</span>");
            html.AppendLine($"                </div>");
            html.AppendLine($"                <div class=\"test-metrics\">");
            html.AppendLine($"                    <div class=\"test-metric\">");
            html.AppendLine($"                        <span class=\"metric-label\">Requests</span>");
            html.AppendLine($"                        <span class=\"metric-value\">{result.TotalRequests:N0}</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                    <div class=\"test-metric\">");
            html.AppendLine($"                        <span class=\"metric-label\">Success Rate</span>");
            html.AppendLine($"                        <span class=\"metric-value\">{(100 - result.ErrorRatePercent):F1}%</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                    <div class=\"test-metric\">");
            html.AppendLine($"                        <span class=\"metric-label\">Avg Response</span>");
            html.AppendLine($"                        <span class=\"metric-value\">{result.AverageResponseTimeMs:F0}ms</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                    <div class=\"test-metric\">");
            html.AppendLine($"                        <span class=\"metric-label\">Throughput</span>");
            html.AppendLine($"                        <span class=\"metric-value\">{result.RequestsPerSecond:F1}/s</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                </div>");
            html.AppendLine($"                <div class=\"test-details\">");
            html.AppendLine($"                    <div class=\"detail-row\">");
            html.AppendLine($"                        <span>Duration:</span>");
            html.AppendLine($"                        <span>{result.DurationSeconds?.ToString("F1") ?? "N/A"}s</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                    <div class=\"detail-row\">");
            html.AppendLine($"                        <span>CPU Usage:</span>");
            html.AppendLine($"                        <span>{result.CpuUsagePercent:F1}%</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                    <div class=\"detail-row\">");
            html.AppendLine($"                        <span>Memory Usage:</span>");
            html.AppendLine($"                        <span>{result.MemoryUsagePercent:F1}%</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                    <div class=\"detail-row\">");
            html.AppendLine($"                        <span>Impact Level:</span>");
            html.AppendLine($"                        <span class=\"impact-{result.PerformanceImpact.ToString().ToLower()}\">{result.PerformanceImpact}</span>");
            html.AppendLine($"                    </div>");
            html.AppendLine($"                </div>");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                html.AppendLine($"                <div class=\"test-error\">");
                html.AppendLine($"                    <strong>Error:</strong> {result.ErrorMessage}");
                html.AppendLine($"                </div>");
            }
            html.AppendLine($"            </div>");
        }
        
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");

        // Overall Summary Cards AFTER individual results
        html.AppendLine("    <div class=\"summary-section\">");
        html.AppendLine("        <h2>📊 Overall Summary</h2>");
        html.AppendLine("        <div class=\"summary-cards\">");
        html.AppendLine("            <div class=\"card success-card\">");
        html.AppendLine("                <div class=\"card-icon\">✅</div>");
        html.AppendLine("                <h3>Success Rate</h3>");
        html.AppendLine($"                <div class=\"metric-value\">{(100 - overallErrorRate):F2}%</div>");
        html.AppendLine($"                <div class=\"metric-detail\">{totalSuccessfulRequests:N0} / {totalRequests:N0}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"card performance-card\">");
        html.AppendLine("                <div class=\"card-icon\">⚡</div>");
        html.AppendLine("                <h3>Avg Response Time</h3>");
        html.AppendLine($"                <div class=\"metric-value\">{averageResponseTimeMs:F2} ms</div>");
        html.AppendLine($"                <div class=\"metric-detail\">Across all tests</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"card throughput-card\">");
        html.AppendLine("                <div class=\"card-icon\">📈</div>");
        html.AppendLine("                <h3>Avg Throughput</h3>");
        html.AppendLine($"                <div class=\"metric-value\">{averageRequestsPerSecond:F2}</div>");
        html.AppendLine($"                <div class=\"metric-detail\">requests/second</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"card system-card\">");
        html.AppendLine("                <div class=\"card-icon\">💻</div>");
        html.AppendLine("                <h3>System Usage</h3>");
        html.AppendLine($"                <div class=\"metric-value\">{averageCpuUsage:F1}% CPU</div>");
        html.AppendLine($"                <div class=\"metric-detail\">{averageMemoryUsage:F1}% Memory</div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");

        // Charts Section AFTER summaries
        html.AppendLine("    <div class=\"charts-section\">");
        html.AppendLine("        <h2>📈 Performance Charts</h2>");
        html.AppendLine("        <div class=\"charts-grid\">");
        html.AppendLine("            <div class=\"chart-container\">");
        html.AppendLine("                <h3>Response Time Distribution</h3>");
        html.AppendLine("                <canvas id=\"responseTimeChart\"></canvas>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"chart-container\">");
        html.AppendLine("                <h3>Success Rate by Test</h3>");
        html.AppendLine("                <canvas id=\"successRateChart\"></canvas>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        
        // Footer
        html.AppendLine("    <div class=\"footer\">");
        html.AppendLine("        <p>Generated by StressLab Performance Testing Framework</p>");
        html.AppendLine("        <p>Report generated on " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC</p>");
        html.AppendLine("    </div>");
        
        // JavaScript for charts
        html.AppendLine("    <script>");
        html.AppendLine(GenerateChartScript(resultsList));
        html.AppendLine("    </script>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    private string GetModernCssStyles()
    {
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Inter', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            color: #333;
            line-height: 1.6;
        }
        
        .header {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            padding: 40px 20px;
            text-align: center;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }
        
        .header-content h1 {
            font-size: 3rem;
            font-weight: 700;
            background: linear-gradient(135deg, #667eea, #764ba2);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-bottom: 20px;
        }
        
        .header-stats {
            display: flex;
            justify-content: center;
            gap: 40px;
            margin: 30px 0;
            flex-wrap: wrap;
        }
        
        .header-stat {
            text-align: center;
        }
        
        .stat-number {
            display: block;
            font-size: 2.5rem;
            font-weight: 700;
            color: #667eea;
        }
        
        .stat-label {
            display: block;
            font-size: 0.9rem;
            color: #666;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        
        .report-date {
            color: #666;
            font-size: 0.9rem;
        }
        
        .summary-section, .charts-section, .tests-section {
            max-width: 1400px;
            margin: 40px auto;
            padding: 0 20px;
        }
        
        .summary-section h2, .charts-section h2, .tests-section h2 {
            color: white;
            font-size: 2rem;
            margin-bottom: 30px;
            text-align: center;
        }
        
        .summary-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 25px;
            margin-bottom: 40px;
        }
        
        .card {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            padding: 30px;
            border-radius: 20px;
            text-align: center;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s ease, box-shadow 0.3s ease;
        }
        
        .card:hover {
            transform: translateY(-5px);
            box-shadow: 0 12px 40px rgba(0, 0, 0, 0.15);
        }
        
        .card-icon {
            font-size: 3rem;
            margin-bottom: 15px;
        }
        
        .card h3 {
            font-size: 1.2rem;
            color: #666;
            margin-bottom: 15px;
            font-weight: 600;
        }
        
        .metric-value {
            font-size: 2.5rem;
            font-weight: 700;
            margin-bottom: 10px;
        }
        
        .success-card .metric-value { color: #4CAF50; }
        .performance-card .metric-value { color: #FF9800; }
        .throughput-card .metric-value { color: #2196F3; }
        .system-card .metric-value { color: #9C27B0; }
        
        .metric-detail {
            font-size: 0.9rem;
            color: #666;
        }
        
        .charts-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
            gap: 30px;
        }
        
        .chart-container {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            padding: 30px;
            border-radius: 20px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }
        
        .chart-container h3 {
            color: #333;
            margin-bottom: 20px;
            text-align: center;
        }
        
        .tests-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
            gap: 25px;
        }
        
        .test-card {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            border-radius: 20px;
            padding: 25px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s ease;
        }
        
        .test-card:hover {
            transform: translateY(-3px);
        }
        
        .test-card.success {
            border-left: 5px solid #4CAF50;
        }
        
        .test-card.failed {
            border-left: 5px solid #f44336;
        }
        
        .test-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }
        
        .test-header h3 {
            font-size: 1.3rem;
            color: #333;
        }
        
        .test-status {
            padding: 5px 15px;
            border-radius: 20px;
            font-size: 0.8rem;
            font-weight: 600;
            text-transform: uppercase;
        }
        
        .test-card.success .test-status {
            background: #E8F5E8;
            color: #4CAF50;
        }
        
        .test-card.failed .test-status {
            background: #FFEBEE;
            color: #f44336;
        }
        
        .test-metrics {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 15px;
            margin-bottom: 20px;
        }
        
        .test-metric {
            text-align: center;
        }
        
        .test-metric .metric-label {
            display: block;
            font-size: 0.8rem;
            color: #666;
            margin-bottom: 5px;
        }
        
        .test-metric .metric-value {
            display: block;
            font-size: 1.5rem;
            font-weight: 700;
            color: #667eea;
        }
        
        .test-details {
            border-top: 1px solid #eee;
            padding-top: 15px;
        }
        
        .detail-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
            font-size: 0.9rem;
        }
        
        .detail-row span:first-child {
            color: #666;
        }
        
        .detail-row span:last-child {
            font-weight: 600;
        }
        
        .impact-minor { color: #4CAF50; }
        .impact-moderate { color: #FF9800; }
        .impact-major { color: #FF5722; }
        .impact-critical { color: #f44336; }
        
        .test-error {
            background: #FFEBEE;
            color: #f44336;
            padding: 15px;
            border-radius: 10px;
            margin-top: 15px;
            font-size: 0.9rem;
        }
        
        .footer {
            background: rgba(0, 0, 0, 0.8);
            color: white;
            text-align: center;
            padding: 30px;
            margin-top: 60px;
        }
        
        .footer p {
            margin: 5px 0;
        }
        
        @media (max-width: 768px) {
            .header-content h1 {
                font-size: 2rem;
            }
            
            .header-stats {
                gap: 20px;
            }
            
            .stat-number {
                font-size: 2rem;
            }
            
            .summary-cards, .tests-grid {
                grid-template-columns: 1fr;
            }
            
            .charts-grid {
                grid-template-columns: 1fr;
            }
            
            .test-metrics {
                grid-template-columns: 1fr;
            }
        }
        ";
    }

    private string GenerateChartScript(IEnumerable<TestResult> results)
    {
        var resultsList = results.ToList();
        var testNames = resultsList.Select(r => r.TestName).ToArray();
        var responseTimes = resultsList.Select(r => r.AverageResponseTimeMs).ToArray();
        var successRates = resultsList.Select(r => 100 - r.ErrorRatePercent).ToArray();
        
        return $@"
        // Response Time Chart
        const responseTimeCtx = document.getElementById('responseTimeChart').getContext('2d');
        new Chart(responseTimeCtx, {{
            type: 'bar',
            data: {{
                labels: {JsonSerializer.Serialize(testNames)},
                datasets: [{{
                    label: 'Average Response Time (ms)',
                    data: {JsonSerializer.Serialize(responseTimes)},
                    backgroundColor: 'rgba(102, 126, 234, 0.8)',
                    borderColor: 'rgba(102, 126, 234, 1)',
                    borderWidth: 2,
                    borderRadius: 8,
                    borderSkipped: false,
                }}]
            }},
            options: {{
                responsive: true,
                plugins: {{
                    legend: {{
                        display: false
                    }}
                }},
                scales: {{
                    y: {{
                        beginAtZero: true,
                        grid: {{
                            color: 'rgba(0, 0, 0, 0.1)'
                        }}
                    }},
                    x: {{
                        grid: {{
                            display: false
                        }}
                    }}
                }}
            }}
        }});
        
        // Success Rate Chart
        const successRateCtx = document.getElementById('successRateChart').getContext('2d');
        new Chart(successRateCtx, {{
            type: 'doughnut',
            data: {{
                labels: {JsonSerializer.Serialize(testNames)},
                datasets: [{{
                    data: {JsonSerializer.Serialize(successRates)},
                    backgroundColor: [
                        '#4CAF50',
                        '#2196F3',
                        '#FF9800',
                        '#9C27B0',
                        '#f44336',
                        '#00BCD4',
                        '#8BC34A',
                        '#FFC107'
                    ],
                    borderWidth: 0
                }}]
            }},
            options: {{
                responsive: true,
                plugins: {{
                    legend: {{
                        position: 'bottom',
                        labels: {{
                            padding: 20,
                            usePointStyle: true
                        }}
                    }}
                }}
            }}
        }});
        ";
    }
}
