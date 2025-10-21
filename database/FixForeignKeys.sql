-- Fix Foreign Key Constraints for TestResultHistory table
-- This script allows NULL values in foreign key columns for scenarios

-- Drop existing foreign key constraints (they may have random names)
DECLARE @sql NVARCHAR(MAX) = '';

-- Find and drop TestConfigurationId foreign key
SELECT @sql = @sql + 'ALTER TABLE TestResultHistory DROP CONSTRAINT ' + name + ';' + CHAR(13)
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('TestResultHistory') 
  AND referenced_object_id = OBJECT_ID('TestConfigurations');

-- Find and drop TestResultId foreign key  
SELECT @sql = @sql + 'ALTER TABLE TestResultHistory DROP CONSTRAINT ' + name + ';' + CHAR(13)
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('TestResultHistory') 
  AND referenced_object_id = OBJECT_ID('TestResults');

-- Execute the DROP statements
IF @sql != ''
BEGIN
    PRINT 'Dropping existing foreign key constraints...';
    EXEC sp_executesql @sql;
END

-- Add new foreign key constraints that allow NULL values
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TestResultHistory_TestConfiguration')
BEGIN
    PRINT 'Adding FK_TestResultHistory_TestConfiguration constraint...';
    ALTER TABLE TestResultHistory 
    ADD CONSTRAINT FK_TestResultHistory_TestConfiguration 
        FOREIGN KEY (TestConfigurationId) REFERENCES TestConfigurations(Id);
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TestResultHistory_TestResult')
BEGIN
    PRINT 'Adding FK_TestResultHistory_TestResult constraint...';
    ALTER TABLE TestResultHistory 
    ADD CONSTRAINT FK_TestResultHistory_TestResult 
        FOREIGN KEY (TestResultId) REFERENCES TestResults(Id);
END

PRINT 'Foreign key constraints updated successfully!';
