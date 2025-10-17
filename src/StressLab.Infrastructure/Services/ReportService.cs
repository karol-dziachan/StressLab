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

    public ReportService(ILogger<ReportService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateHtmlReportAsync(TestResult result, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating HTML report for test result: {ResultId}", result.Id);

        try
        {
            var htmlContent = GenerateHtmlContent(result);
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

    private string GenerateHtmlContent(TestResult result)
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
        if (!string.IsNullOrEmpty(result.Notes))
        {
            html.AppendLine("        <div class=\"notes\">");
            html.AppendLine($"            <h4>Notes</h4>");
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
}
