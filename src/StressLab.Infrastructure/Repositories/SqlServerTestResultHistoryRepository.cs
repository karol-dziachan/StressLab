using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StressLab.Core.Entities;
using StressLab.Core.Enums;
using StressLab.Core.Interfaces.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;

namespace StressLab.Infrastructure.Repositories;

/// <summary>
/// SQL Server repository for test result history using Dapper
/// </summary>
public class SqlServerTestResultHistoryRepository : ITestResultHistoryRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerTestResultHistoryRepository> _logger;

    public SqlServerTestResultHistoryRepository(
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<SqlServerTestResultHistoryRepository> logger)
    {
        _connectionString = databaseOptions.Value.ConnectionString;
        _logger = logger;
        
        _logger.LogInformation("🔗 Database connection configured: {ConnectionString}", 
            _connectionString.Replace("Password=", "Password=***"));
        
        // Test database connection on startup
        _ = Task.Run(async () => await TestDatabaseConnectionAsync());
    }
    
    private async Task TestDatabaseConnectionAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Testing database connection...");
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.CloseAsync();
            _logger.LogInformation("✅ Database connection test successful!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CRITICAL: Database connection test FAILED!");
            _logger.LogError("Please check your connection string and ensure SQL Server is running");
        }
    }

    public async Task<IEnumerable<TestResultHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all historical test results from database");
        
        const string sql = @"
            SELECT Id, TestName, ExecutionDate, DurationSeconds, TotalRequests, 
                   SuccessfulRequests, FailedRequests, ErrorRatePercent, 
                   AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, 
                   P95ResponseTimeMs, P99ResponseTimeMs, RequestsPerSecond, 
                   CpuUsagePercent, MemoryUsagePercent, PerformanceImpact, 
                   Status, TestConfigurationId, TestResultId
            FROM TestResultHistory 
            ORDER BY ExecutionDate DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var results = await connection.QueryAsync<TestResultHistory>(sql);
        return results;
    }

    public async Task<IEnumerable<TestResultHistory>> GetByTestNameAsync(string testName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving historical test results for test: {TestName}", testName);
        
        const string sql = @"
            SELECT Id, TestName, ExecutionDate, DurationSeconds, TotalRequests, 
                   SuccessfulRequests, FailedRequests, ErrorRatePercent, 
                   AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, 
                   P95ResponseTimeMs, P99ResponseTimeMs, RequestsPerSecond, 
                   CpuUsagePercent, MemoryUsagePercent, PerformanceImpact, 
                   Status, TestConfigurationId, TestResultId
            FROM TestResultHistory 
            WHERE TestName = @TestName
            ORDER BY ExecutionDate DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var results = await connection.QueryAsync<TestResultHistory>(sql, new { TestName = testName });
        return results;
    }

    public async Task<IEnumerable<TestResultHistory>> GetByTestNameAndDateRangeAsync(string testName, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving historical test results for test: {TestName} from {FromDate} to {ToDate}", testName, fromDate, toDate);
        
        const string sql = @"
            SELECT Id, TestName, ExecutionDate, DurationSeconds, TotalRequests, 
                   SuccessfulRequests, FailedRequests, ErrorRatePercent, 
                   AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, 
                   P95ResponseTimeMs, P99ResponseTimeMs, RequestsPerSecond, 
                   CpuUsagePercent, MemoryUsagePercent, PerformanceImpact, 
                   Status, TestConfigurationId, TestResultId
            FROM TestResultHistory 
            WHERE TestName = @TestName 
              AND ExecutionDate >= @FromDate 
              AND ExecutionDate <= @ToDate
            ORDER BY ExecutionDate DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var results = await connection.QueryAsync<TestResultHistory>(sql, new { 
            TestName = testName, 
            FromDate = fromDate, 
            ToDate = toDate 
        });
        return results;
    }

    public async Task<IEnumerable<TestResultHistory>> GetRecentByTestNameAsync(string testName, int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving {Count} recent historical test results for test: {TestName}", count, testName);
        
        const string sql = @"
            SELECT TOP (@Count) Id, TestName, ExecutionDate, DurationSeconds, TotalRequests, 
                   SuccessfulRequests, FailedRequests, ErrorRatePercent, 
                   AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, 
                   P95ResponseTimeMs, P99ResponseTimeMs, RequestsPerSecond, 
                   CpuUsagePercent, MemoryUsagePercent, PerformanceImpact, 
                   Status, TestConfigurationId, TestResultId
            FROM TestResultHistory 
            WHERE TestName = @TestName
            ORDER BY ExecutionDate DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var results = await connection.QueryAsync<TestResultHistory>(sql, new { 
            TestName = testName, 
            Count = count 
        });
        return results;
    }

    public async Task<TestResultHistory> CreateAsync(TestResultHistory history, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating historical test result: {Id} for test: {TestName}", history.Id, history.TestName);
            
            const string sql = @"
                INSERT INTO TestResultHistory (
                    Id, TestName, ExecutionDate, DurationSeconds, TotalRequests, 
                    SuccessfulRequests, FailedRequests, ErrorRatePercent, 
                    AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, 
                    P95ResponseTimeMs, P99ResponseTimeMs, RequestsPerSecond, 
                    CpuUsagePercent, MemoryUsagePercent, PerformanceImpact, 
                    Status, TestConfigurationId, TestResultId
                ) VALUES (
                    @Id, @TestName, @ExecutionDate, @DurationSeconds, @TotalRequests, 
                    @SuccessfulRequests, @FailedRequests, @ErrorRatePercent, 
                    @AverageResponseTimeMs, @MinResponseTimeMs, @MaxResponseTimeMs, 
                    @P95ResponseTimeMs, @P99ResponseTimeMs, @RequestsPerSecond, 
                    @CpuUsagePercent, @MemoryUsagePercent, @PerformanceImpact, 
                    @Status, @TestConfigurationId, @TestResultId
                )";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            var rowsAffected = await connection.ExecuteAsync(sql, new {
                Id = history.Id,
                TestName = history.TestName,
                ExecutionDate = history.ExecutionDate,
                DurationSeconds = history.DurationSeconds,
                TotalRequests = history.TotalRequests,
                SuccessfulRequests = history.SuccessfulRequests,
                FailedRequests = history.FailedRequests,
                ErrorRatePercent = history.ErrorRatePercent,
                AverageResponseTimeMs = history.AverageResponseTimeMs,
                MinResponseTimeMs = history.MinResponseTimeMs,
                MaxResponseTimeMs = history.MaxResponseTimeMs,
                P95ResponseTimeMs = history.P95ResponseTimeMs,
                P99ResponseTimeMs = history.P99ResponseTimeMs,
                RequestsPerSecond = history.RequestsPerSecond,
                CpuUsagePercent = history.CpuUsagePercent,
                MemoryUsagePercent = history.MemoryUsagePercent,
                PerformanceImpact = (int)history.PerformanceImpact,
                Status = (int)history.Status,
                TestConfigurationId = history.TestConfigurationId,
                TestResultId = history.TestResultId
            });
            
            if (rowsAffected == 0)
            {
                var errorMsg = $"Failed to insert test result history - no rows affected for test: {history.TestName}";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            _logger.LogInformation("✅ Successfully saved test result to database: {TestName} (ID: {Id})", history.TestName, history.Id);
            return history;
        }
        catch (Exception ex)
        {
            var errorMsg = $"❌ CRITICAL ERROR: Failed to save test result to database for test: {history.TestName} (ID: {history.Id})";
            _logger.LogError(ex, errorMsg);
            _logger.LogError("Connection string: {ConnectionString}", _connectionString);
            throw new InvalidOperationException(errorMsg, ex);
        }
    }

    public async Task<TestResultHistory?> GetBaselineAsync(string testName, int sampleSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calculating baseline for test: {TestName} using {SampleSize} samples", testName, sampleSize);
        
        const string sql = @"
            SELECT TOP (@SampleSize) Id, TestName, ExecutionDate, DurationSeconds, TotalRequests, 
                   SuccessfulRequests, FailedRequests, ErrorRatePercent, 
                   AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, 
                   P95ResponseTimeMs, P99ResponseTimeMs, RequestsPerSecond, 
                   CpuUsagePercent, MemoryUsagePercent, PerformanceImpact, 
                   Status, TestConfigurationId, TestResultId
            FROM TestResultHistory 
            WHERE TestName = @TestName 
              AND Status = @Status
            ORDER BY ExecutionDate DESC";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var recentResults = await connection.QueryAsync<TestResultHistory>(sql, new { 
            TestName = testName, 
            SampleSize = sampleSize,
            Status = (int)TestStatus.Completed
        });
        
        var resultsList = recentResults.ToList();
        
        if (resultsList.Count < 3) // Need at least 3 samples for meaningful baseline
        {
            _logger.LogDebug("Insufficient data for baseline calculation: {Count} samples", resultsList.Count);
            return null;
        }
        
        // Calculate average values for baseline
        var baseline = new TestResultHistory
        {
            Id = Guid.NewGuid(),
            TestName = testName,
            ExecutionDate = DateTime.UtcNow,
            DurationSeconds = resultsList.Average(r => r.DurationSeconds),
            TotalRequests = (long)resultsList.Average(r => r.TotalRequests),
            SuccessfulRequests = (long)resultsList.Average(r => r.SuccessfulRequests),
            FailedRequests = (long)resultsList.Average(r => r.FailedRequests),
            ErrorRatePercent = resultsList.Average(r => r.ErrorRatePercent),
            AverageResponseTimeMs = resultsList.Average(r => r.AverageResponseTimeMs),
            MinResponseTimeMs = resultsList.Average(r => r.MinResponseTimeMs),
            MaxResponseTimeMs = resultsList.Average(r => r.MaxResponseTimeMs),
            P95ResponseTimeMs = resultsList.Average(r => r.P95ResponseTimeMs),
            P99ResponseTimeMs = resultsList.Average(r => r.P99ResponseTimeMs),
            RequestsPerSecond = resultsList.Average(r => r.RequestsPerSecond),
            CpuUsagePercent = resultsList.Average(r => r.CpuUsagePercent),
            MemoryUsagePercent = resultsList.Average(r => r.MemoryUsagePercent),
            PerformanceImpact = resultsList.GroupBy(r => r.PerformanceImpact)
                .OrderByDescending(g => g.Count())
                .First().Key,
            Status = TestStatus.Completed
        };
        
        _logger.LogDebug("Baseline calculated for test: {TestName} using {SampleSize} samples", testName, resultsList.Count);
        return baseline;
    }

    public async Task<int> CleanupOldRecordsAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up historical records older than {RetentionDays} days", retentionDays);
        
        const string sql = @"
            DELETE FROM TestResultHistory 
            WHERE ExecutionDate < @CutoffDate";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedCount = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate });
        
        _logger.LogDebug("Cleaned up {DeletedCount} historical records", deletedCount);
        return deletedCount;
    }
}

/// <summary>
/// Database configuration options
/// </summary>
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
