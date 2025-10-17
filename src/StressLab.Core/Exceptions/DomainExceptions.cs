namespace StressLab.Core.Exceptions;

/// <summary>
/// Base exception for all StressLab domain exceptions
/// </summary>
public abstract class StressLabException : Exception
{
    protected StressLabException(string message) : base(message)
    {
    }
    
    protected StressLabException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Base exception for domain-related errors
/// </summary>
public class DomainException : StressLabException
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a test configuration is invalid
/// </summary>
public class InvalidTestConfigurationException : StressLabException
{
    public InvalidTestConfigurationException(string message) : base(message)
    {
    }
    
    public InvalidTestConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a test execution fails
/// </summary>
public class TestExecutionException : StressLabException
{
    public TestExecutionException(string message) : base(message)
    {
    }
    
    public TestExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a test configuration is not found
/// </summary>
public class TestConfigurationNotFoundException : StressLabException
{
    public TestConfigurationNotFoundException(Guid configurationId) 
        : base($"Test configuration with ID '{configurationId}' was not found.")
    {
    }
}

/// <summary>
/// Exception thrown when a test result is not found
/// </summary>
public class TestResultNotFoundException : StressLabException
{
    public TestResultNotFoundException(Guid resultId) 
        : base($"Test result with ID '{resultId}' was not found.")
    {
    }
}

/// <summary>
/// Exception thrown when database connection fails
/// </summary>
public class DatabaseConnectionException : StressLabException
{
    public DatabaseConnectionException(string message) : base(message)
    {
    }
    
    public DatabaseConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when API endpoint is not accessible
/// </summary>
public class ApiEndpointException : StressLabException
{
    public ApiEndpointException(string message) : base(message)
    {
    }
    
    public ApiEndpointException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
