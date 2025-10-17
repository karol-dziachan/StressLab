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
    private readonly bool _isWindows;

    public SystemMetricsService(ILogger<SystemMetricsService> logger)
    {
        _logger = logger;
        _isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        
        if (_isWindows)
        {
            try
            {
                // Initialize Windows performance counters
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
                _logger.LogWarning(ex, "Failed to initialize Windows performance counters. Some metrics may not be available.");
            }
        }
        else
        {
            _logger.LogInformation("Running on Linux - using /proc filesystem for system metrics");
        }
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting system metrics monitoring");
        
        _isMonitoring = true;
        _monitoringStartTime = DateTime.UtcNow;
        
        if (_isWindows)
        {
            // Initialize Windows network counters
            if (_networkSentCounter is not null)
            {
                _initialNetworkSent = (long)_networkSentCounter.NextValue();
            }
            
            if (_networkReceivedCounter is not null)
            {
                _initialNetworkReceived = (long)_networkReceivedCounter.NextValue();
            }
        }
        else
        {
            // Initialize Linux network counters
            _initialNetworkSent = GetLinuxNetworkBytesSent();
            _initialNetworkReceived = GetLinuxNetworkBytesReceived();
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
            double cpuUsage = 0;
            double memoryUsagePercent = 0;
            long availableMemoryBytes = 0;
            long totalMemoryBytes = 0;
            double diskUsage = 0;
            long networkSent = 0;
            long networkReceived = 0;
            
            if (_isWindows)
            {
                // Windows implementation using Performance Counters
                cpuUsage = _cpuCounter?.NextValue() ?? 0;
                var availableMemoryMB = _memoryCounter?.NextValue() ?? 0;
                diskUsage = _diskCounter?.NextValue() ?? 0;
                
                // Get total memory
                totalMemoryBytes = GC.GetTotalMemory(false);
                availableMemoryBytes = (long)(availableMemoryMB * 1024 * 1024);
                memoryUsagePercent = totalMemoryBytes > 0 ? 
                    ((double)(totalMemoryBytes - availableMemoryBytes) / totalMemoryBytes) * 100 : 0;
                
                // Get network metrics
                networkSent = (long)(_networkSentCounter?.NextValue() ?? 0);
                networkReceived = (long)(_networkReceivedCounter?.NextValue() ?? 0);
            }
            else
            {
                // Linux implementation using /proc filesystem
                cpuUsage = GetLinuxCpuUsage();
                (memoryUsagePercent, availableMemoryBytes, totalMemoryBytes) = GetLinuxMemoryUsage();
                diskUsage = GetLinuxDiskUsage();
                networkSent = GetLinuxNetworkBytesSent();
                networkReceived = GetLinuxNetworkBytesReceived();
            }
            
            var metrics = new SystemMetrics
            {
                CpuUsagePercent = Math.Round(cpuUsage, 2),
                MemoryUsagePercent = Math.Round(memoryUsagePercent, 2),
                AvailableMemoryBytes = availableMemoryBytes,
                TotalMemoryBytes = totalMemoryBytes,
                DiskUsagePercent = Math.Round(diskUsage, 2),
                NetworkBytesSent = networkSent,
                NetworkBytesReceived = networkReceived,
                Timestamp = DateTime.UtcNow
            };
            
            _currentMetrics = metrics;
            return metrics;
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

    // Linux-specific methods using /proc filesystem
    private double GetLinuxCpuUsage()
    {
        try
        {
            var stat1 = File.ReadAllText("/proc/stat");
            var lines1 = stat1.Split('\n');
            var cpuLine1 = lines1[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var idle1 = long.Parse(cpuLine1[4]);
            var total1 = cpuLine1.Skip(1).Take(7).Sum(x => long.Parse(x));
            
            Thread.Sleep(100); // Wait 100ms
            
            var stat2 = File.ReadAllText("/proc/stat");
            var lines2 = stat2.Split('\n');
            var cpuLine2 = lines2[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var idle2 = long.Parse(cpuLine2[4]);
            var total2 = cpuLine2.Skip(1).Take(7).Sum(x => long.Parse(x));
            
            var idleDiff = idle2 - idle1;
            var totalDiff = total2 - total1;
            
            if (totalDiff == 0) return 0;
            
            return 100.0 * (1.0 - (double)idleDiff / totalDiff);
        }
        catch
        {
            return 0;
        }
    }

    private (double usagePercent, long availableBytes, long totalBytes) GetLinuxMemoryUsage()
    {
        try
        {
            var memInfo = File.ReadAllText("/proc/meminfo");
            var lines = memInfo.Split('\n');
            
            long totalMem = 0;
            long availableMem = 0;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    totalMem = long.Parse(parts[1]) * 1024; // Convert from KB to bytes
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    availableMem = long.Parse(parts[1]) * 1024; // Convert from KB to bytes
                }
            }
            
            if (totalMem == 0) return (0, 0, 0);
            
            var usedMem = totalMem - availableMem;
            var usagePercent = (double)usedMem / totalMem * 100;
            
            return (usagePercent, availableMem, totalMem);
        }
        catch
        {
            return (0, 0, 0);
        }
    }

    private double GetLinuxDiskUsage()
    {
        try
        {
            var dfOutput = File.ReadAllText("/proc/diskstats");
            // Simplified disk usage - in real implementation you'd parse /proc/diskstats
            // For now, return 0 as disk I/O monitoring is complex
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private long GetLinuxNetworkBytesSent()
    {
        try
        {
            var netDev = File.ReadAllText("/proc/net/dev");
            var lines = netDev.Split('\n');
            
            long totalSent = 0;
            foreach (var line in lines.Skip(2)) // Skip header lines
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                
                var interfaceName = parts[0].Trim();
                if (interfaceName == "lo") continue; // Skip loopback
                
                var stats = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (stats.Length >= 9)
                {
                    totalSent += long.Parse(stats[8]); // bytes sent
                }
            }
            
            return totalSent;
        }
        catch
        {
            return 0;
        }
    }

    private long GetLinuxNetworkBytesReceived()
    {
        try
        {
            var netDev = File.ReadAllText("/proc/net/dev");
            var lines = netDev.Split('\n');
            
            long totalReceived = 0;
            foreach (var line in lines.Skip(2)) // Skip header lines
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                
                var interfaceName = parts[0].Trim();
                if (interfaceName == "lo") continue; // Skip loopback
                
                var stats = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (stats.Length >= 1)
                {
                    totalReceived += long.Parse(stats[0]); // bytes received
                }
            }
            
            return totalReceived;
        }
        catch
        {
            return 0;
        }
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
