using System.Collections.Generic;

namespace StressLab.Core.Entities;

/// <summary>
/// Configuration for HttpClient used in performance tests
/// </summary>
public record HttpClientConfiguration
{
    /// <summary>
    /// Default headers to be added to all HTTP requests
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; init; } = new();

    /// <summary>
    /// Default timeout for HTTP requests in milliseconds
    /// </summary>
    public int TimeoutMs { get; init; } = 30000;

    /// <summary>
    /// Base URL for all requests (optional)
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// User agent string for all requests
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Whether to follow redirects automatically
    /// </summary>
    public bool FollowRedirects { get; init; } = true;

    /// <summary>
    /// Maximum number of redirects to follow
    /// </summary>
    public int MaxRedirects { get; init; } = 10;

    /// <summary>
    /// Whether to use compression (gzip, deflate)
    /// </summary>
    public bool UseCompression { get; init; } = true;

    /// <summary>
    /// Connection pool settings
    /// </summary>
    public ConnectionPoolSettings ConnectionPool { get; init; } = new();

    /// <summary>
    /// Proxy settings (optional)
    /// </summary>
    public ProxySettings? Proxy { get; init; }
}

/// <summary>
/// Connection pool settings for HttpClient
/// </summary>
public record ConnectionPoolSettings
{
    /// <summary>
    /// Maximum number of connections per server
    /// </summary>
    public int MaxConnectionsPerServer { get; init; } = 10;

    /// <summary>
    /// Connection lifetime in seconds
    /// </summary>
    public int ConnectionLifetimeSeconds { get; init; } = 300;

    /// <summary>
    /// Idle connection timeout in seconds
    /// </summary>
    public int IdleConnectionTimeoutSeconds { get; init; } = 100;
}

/// <summary>
/// Proxy settings for HttpClient
/// </summary>
public record ProxySettings
{
    /// <summary>
    /// Proxy URL
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Proxy username (optional)
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Proxy password (optional)
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Whether to bypass proxy for local addresses
    /// </summary>
    public bool BypassLocalAddresses { get; init; } = true;
}
