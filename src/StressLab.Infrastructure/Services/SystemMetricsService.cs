using Microsoft.Extensions.Logging;
using StressLab.Core.Interfaces.Services;
using System.Diagnostics;

namespace StressLab.Infrastructure.Services;

/// <summary>
/// Service for monitoring system metrics during performance tests
/// </summary>
public class SystemMetricsService : ISystemMetricsService
{
    private readonly ILogger<SystemMetricsService> _logger;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private readonly PerformanceCounter? _diskCounter;
    private readonly PerformanceCounter? _networkSentCounter;
    private readonly PerformanceCounter? _networkReceivedCounter;
    
    private bool _isMonitoring;
    private DateTime _monitoringStartTime;
    private long _initialNetworkSent;
    private long _initialNetworkReceived;
    private SystemMetrics _currentMetrics = new SystemMetrics();

    public SystemMetricsService(ILogger<SystemMetricsService> logger)
    {
        _logger = logger;
        
        try
        {
            // Initialize performance counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            _networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", GetNetworkInterfaceName());
            _networkReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", GetNetworkInterfaceName());
            
            // Warm up counters
            _cpuCounter.NextValue();
            _memoryCounter.NextValue();
            _diskCounter.NextValue();
            _networkSentCounter.NextValue();
            _networkReceivedCounter.NextValue();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize some performance counters. Some metrics may not be available.");
        }
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting system metrics monitoring");
        
        _isMonitoring = true;
        _monitoringStartTime = DateTime.UtcNow;
        
        // Initialize network counters
        if (_networkSentCounter is not null)
        {
            _initialNetworkSent = (long)_networkSentCounter.NextValue();
        }
        
        if (_networkReceivedCounter is not null)
        {
            _initialNetworkReceived = (long)_networkReceivedCounter.NextValue();
        }
        
        await Task.CompletedTask;
    }

    public async Task<SystemMetrics> StopMonitoringAsync()
    {
        _logger.LogInformation("Stopping system metrics monitoring");
        
        _isMonitoring = false;
        
        var metrics = GetCurrentMetrics();
        
        await Task.CompletedTask;
        
        return metrics;
    }

    public SystemMetrics GetCurrentMetrics()
    {
        try
        {
            var cpuUsage = _cpuCounter?.NextValue() ?? 0;
            var availableMemoryMB = _memoryCounter?.NextValue() ?? 0;
            var diskUsage = _diskCounter?.NextValue() ?? 0;
            
            // Get total memory
            var totalMemoryBytes = GC.GetTotalMemory(false);
            var availableMemoryBytes = (long)(availableMemoryMB * 1024 * 1024);
            var memoryUsagePercent = totalMemoryBytes > 0 ? 
                ((double)(totalMemoryBytes - availableMemoryBytes) / totalMemoryBytes) * 100 : 0;
            
            // Get network metrics
            var networkSent = _networkSentCounter?.NextValue() ?? 0;
            var networkReceived = _networkReceivedCounter?.NextValue() ?? 0;
            
            return new SystemMetrics
            {
                CpuUsagePercent = Math.Round(cpuUsage, 2),
                MemoryUsagePercent = Math.Round(memoryUsagePercent, 2),
                AvailableMemoryBytes = availableMemoryBytes,
                TotalMemoryBytes = totalMemoryBytes,
                DiskUsagePercent = Math.Round(diskUsage, 2),
                NetworkBytesSent = (long)networkSent,
                NetworkBytesReceived = (long)networkReceived,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
            
            return new SystemMetrics
            {
                CpuUsagePercent = 0,
                MemoryUsagePercent = 0,
                AvailableMemoryBytes = 0,
                TotalMemoryBytes = 0,
                DiskUsagePercent = 0,
                NetworkBytesSent = 0,
                NetworkBytesReceived = 0,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private static string GetNetworkInterfaceName()
    {
        try
        {
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var activeInterface = networkInterfaces.FirstOrDefault(ni => 
                ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);
            
            return activeInterface?.Name ?? "Ethernet";
        }
        catch
        {
            return "Ethernet";
        }
    }

    public SystemMetrics GetMetrics()
    {
        return _currentMetrics;
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        _diskCounter?.Dispose();
        _networkSentCounter?.Dispose();
        _networkReceivedCounter?.Dispose();
    }
}
