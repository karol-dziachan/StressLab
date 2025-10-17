using Microsoft.Extensions.Logging;
using StressLab.Core.Entities;
using StressLab.Core.Interfaces.Services;
using System.Net;
using System.Net.Http;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for configuring HttpClient used in performance tests
/// </summary>
public class HttpClientConfigurationService : IHttpClientConfigurationService
{
    private readonly ILogger<HttpClientConfigurationService> _logger;

    public HttpClientConfigurationService(ILogger<HttpClientConfigurationService> logger)
    {
        _logger = logger;
    }

    public void ConfigureHttpClient(HttpClient httpClient, HttpClientConfiguration configuration)
    {
        _logger.LogInformation("Configuring HttpClient with custom settings");

        try
        {
            // Set timeout
            httpClient.Timeout = TimeSpan.FromMilliseconds(configuration.TimeoutMs);

            // Set base address if provided
            if (!string.IsNullOrEmpty(configuration.BaseUrl))
            {
                httpClient.BaseAddress = new Uri(configuration.BaseUrl);
                _logger.LogInformation("Set base URL: {BaseUrl}", configuration.BaseUrl);
            }

            // Add default headers
            foreach (var header in configuration.DefaultHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                _logger.LogDebug("Added default header: {Key} = {Value}", header.Key, header.Value);
            }

            // Set User-Agent if provided
            if (!string.IsNullOrEmpty(configuration.UserAgent))
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(configuration.UserAgent);
                _logger.LogInformation("Set User-Agent: {UserAgent}", configuration.UserAgent);
            }

            // Configure compression
            if (configuration.UseCompression)
            {
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                _logger.LogDebug("Enabled compression");
            }

            _logger.LogInformation("HttpClient configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure HttpClient");
            throw;
        }
    }

    public HttpClient CreateConfiguredHttpClient(HttpClientConfiguration configuration)
    {
        _logger.LogInformation("Creating new configured HttpClient");

        try
        {
            var handler = CreateHttpClientHandler(configuration);
            var httpClient = new HttpClient(handler);

            ConfigureHttpClient(httpClient, configuration);

            return httpClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create configured HttpClient");
            throw;
        }
    }

    private HttpClientHandler CreateHttpClientHandler(HttpClientConfiguration configuration)
    {
        var handler = new HttpClientHandler();

        // Configure redirects
        handler.AllowAutoRedirect = configuration.FollowRedirects;
        handler.MaxAutomaticRedirections = configuration.MaxRedirects;

        // Configure connection pool
        handler.MaxConnectionsPerServer = configuration.ConnectionPool.MaxConnectionsPerServer;

        // Configure proxy if provided
        if (configuration.Proxy != null)
        {
            ConfigureProxy(handler, configuration.Proxy);
        }

        // Configure SSL/TLS
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

        _logger.LogInformation("HttpClientHandler created with custom settings");
        return handler;
    }

    private void ConfigureProxy(HttpClientHandler handler, ProxySettings proxySettings)
    {
        if (string.IsNullOrEmpty(proxySettings.Url))
        {
            _logger.LogWarning("Proxy URL is empty, skipping proxy configuration");
            return;
        }

        try
        {
            var proxy = new WebProxy(proxySettings.Url);

            // Set credentials if provided
            if (!string.IsNullOrEmpty(proxySettings.Username) && !string.IsNullOrEmpty(proxySettings.Password))
            {
                proxy.Credentials = new NetworkCredential(proxySettings.Username, proxySettings.Password);
                _logger.LogInformation("Configured proxy with credentials: {Url}", proxySettings.Url);
            }
            else
            {
                _logger.LogInformation("Configured proxy without credentials: {Url}", proxySettings.Url);
            }

            // Configure bypass for local addresses
            if (proxySettings.BypassLocalAddresses)
            {
                proxy.BypassProxyOnLocal = true;
                _logger.LogDebug("Enabled proxy bypass for local addresses");
            }

            handler.Proxy = proxy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure proxy: {ProxyUrl}", proxySettings.Url);
            throw;
        }
    }
}
