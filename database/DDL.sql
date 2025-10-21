-- StressLab Database Schema
-- Performance testing framework database tables

-- Test Configurations table
CREATE TABLE TestConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    TestType INT NOT NULL, -- 0=Api, 1=Sql, 2=Combined
    ApiEndpoint NVARCHAR(500),
    ApiMethod NVARCHAR(10),
    SqlConnectionString NVARCHAR(500),
    SqlProcedureName NVARCHAR(255),
    ConcurrentUsers INT NOT NULL DEFAULT 1,
    DurationSeconds INT NOT NULL DEFAULT 60,
    ExpectedResponseTimeMs INT NOT NULL DEFAULT 1000,
    MaxErrorRatePercent DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Test Results table
CREATE TABLE TestResults (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TestConfigurationId UNIQUEIDENTIFIER NOT NULL,
    TestName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Status INT NOT NULL, -- 0=Pending, 1=Running, 2=Completed, 3=Failed
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2,
    DurationSeconds DECIMAL(10,3),
    TotalRequests BIGINT NOT NULL DEFAULT 0,
    SuccessfulRequests BIGINT NOT NULL DEFAULT 0,
    FailedRequests BIGINT NOT NULL DEFAULT 0,
    ErrorRatePercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    AverageResponseTimeMs DECIMAL(10,3) NOT NULL DEFAULT 0,
    MinResponseTimeMs DECIMAL(10,3) NOT NULL DEFAULT 0,
    MaxResponseTimeMs DECIMAL(10,3) NOT NULL DEFAULT 0,
    P95ResponseTimeMs DECIMAL(10,3) NOT NULL DEFAULT 0,
    P99ResponseTimeMs DECIMAL(10,3) NOT NULL DEFAULT 0,
    RequestsPerSecond DECIMAL(10,3) NOT NULL DEFAULT 0,
    CpuUsagePercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    MemoryUsagePercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    PerformanceImpact INT NOT NULL DEFAULT 0, -- 0=None, 1=Minor, 2=Moderate, 3=Major, 4=Critical
    MaxErrorRatePercent DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    ErrorMessage NVARCHAR(MAX),
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (TestConfigurationId) REFERENCES TestConfigurations(Id)
);

-- Test Result History table for tracking historical performance
CREATE TABLE TestResultHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TestName NVARCHAR(255) NOT NULL,
    ExecutionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DurationSeconds DECIMAL(10,3) NOT NULL,
    TotalRequests BIGINT NOT NULL,
    SuccessfulRequests BIGINT NOT NULL,
    FailedRequests BIGINT NOT NULL,
    ErrorRatePercent DECIMAL(5,2) NOT NULL,
    AverageResponseTimeMs DECIMAL(10,3) NOT NULL,
    MinResponseTimeMs DECIMAL(10,3) NOT NULL,
    MaxResponseTimeMs DECIMAL(10,3) NOT NULL,
    P95ResponseTimeMs DECIMAL(10,3) NOT NULL,
    P99ResponseTimeMs DECIMAL(10,3) NOT NULL,
    RequestsPerSecond DECIMAL(10,3) NOT NULL,
    CpuUsagePercent DECIMAL(5,2) NOT NULL,
    MemoryUsagePercent DECIMAL(5,2) NOT NULL,
    PerformanceImpact INT NOT NULL,
    Status INT NOT NULL,
    TestConfigurationId UNIQUEIDENTIFIER,
    TestResultId UNIQUEIDENTIFIER,
    
    -- Indexes for performance
    INDEX IX_TestResultHistory_TestName_ExecutionDate (TestName, ExecutionDate),
    INDEX IX_TestResultHistory_TestName (TestName),
    INDEX IX_TestResultHistory_ExecutionDate (ExecutionDate),
    
    -- Optional foreign keys - only enforce when values are not null
    CONSTRAINT FK_TestResultHistory_TestConfiguration 
        FOREIGN KEY (TestConfigurationId) REFERENCES TestConfigurations(Id),
    CONSTRAINT FK_TestResultHistory_TestResult 
        FOREIGN KEY (TestResultId) REFERENCES TestResults(Id)
);

-- Test Scenarios table
CREATE TABLE TestScenarios (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    Steps NVARCHAR(MAX) NOT NULL, -- JSON serialized steps
    MaxErrorRatePercent DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Performance Analysis Cache table for storing calculated deviations
CREATE TABLE PerformanceAnalysisCache (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TestName NVARCHAR(255) NOT NULL,
    AnalysisDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    BaselineAverageResponseTimeMs DECIMAL(10,3),
    BaselineErrorRatePercent DECIMAL(5,2),
    BaselineRequestsPerSecond DECIMAL(10,3),
    CurrentAverageResponseTimeMs DECIMAL(10,3),
    CurrentErrorRatePercent DECIMAL(5,2),
    CurrentRequestsPerSecond DECIMAL(10,3),
    ResponseTimeDeviationPercent DECIMAL(8,3),
    ErrorRateDeviationPercent DECIMAL(8,3),
    ThroughputDeviationPercent DECIMAL(8,3),
    OverallDeviationScore DECIMAL(8,3),
    TrendDirection INT NOT NULL, -- 0=Stable, 1=Improving, 2=Degrading
    SampleSize INT NOT NULL,
    ConfidenceLevel DECIMAL(5,2),
    Recommendations NVARCHAR(MAX),
    
    INDEX IX_PerformanceAnalysisCache_TestName_AnalysisDate (TestName, AnalysisDate),
    INDEX IX_PerformanceAnalysisCache_TestName (TestName)
);

-- Insert sample test configuration
INSERT INTO TestConfigurations (Id, Name, Description, TestType, ApiEndpoint, ApiMethod, ConcurrentUsers, DurationSeconds, ExpectedResponseTimeMs, MaxErrorRatePercent)
VALUES (NEWID(), 'Sample API Test', 'Basic API performance test', 0, 'https://httpbin.org/delay/1', 'GET', 5, 30, 2000, 5.00);

-- Insert sample test scenario
INSERT INTO TestScenarios (Id, Name, Description, Steps, MaxErrorRatePercent)
VALUES (NEWID(), 'Sample Scenario', 'Basic test scenario', '[{"name":"API Test","type":"Api","endpoint":"https://httpbin.org/delay/1","method":"GET","duration":30,"concurrentUsers":3}]', 5.00);

-- Update script for existing databases (run this if you already have the database)
-- This allows NULL values in foreign key columns for scenarios
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__TestResul__TestC__[random]')
BEGIN
    -- Drop existing foreign key constraints
    ALTER TABLE TestResultHistory DROP CONSTRAINT IF EXISTS FK__TestResul__TestC__[random];
    ALTER TABLE TestResultHistory DROP CONSTRAINT IF EXISTS FK__TestResul__TestR__[random];
END

-- Add new foreign key constraints that allow NULL values
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TestResultHistory_TestConfiguration')
BEGIN
    ALTER TABLE TestResultHistory 
    ADD CONSTRAINT FK_TestResultHistory_TestConfiguration 
        FOREIGN KEY (TestConfigurationId) REFERENCES TestConfigurations(Id);
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TestResultHistory_TestResult')
BEGIN
    ALTER TABLE TestResultHistory 
    ADD CONSTRAINT FK_TestResultHistory_TestResult 
        FOREIGN KEY (TestResultId) REFERENCES TestResults(Id);
END
