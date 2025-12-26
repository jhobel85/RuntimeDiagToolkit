using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Runtime;

namespace DiagnosticsToolkit.Providers.Platforms;

/// <summary>
/// Android runtime metrics provider using /proc sampling and runtime counters.
/// Designed to work on device and emulator without ADB privileges.
/// </summary>
public sealed class DefaultAndroidMetricsProvider : IRuntimeMetricsProvider
{
    private readonly Process _currentProcess;
    private long _lastIdleCpuTicks;
    private long _lastTotalCpuTicks;

    public DefaultAndroidMetricsProvider()
    {
        _currentProcess = Process.GetCurrentProcess();
        TryReadCpuSample(out _lastIdleCpuTicks, out _lastTotalCpuTicks);
        _ = RuntimeCounters.Instance; // initialize runtime counters listener
    }

    public ValueTask<CpuUsage> GetCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        if (!TryReadCpuSample(out var idle, out var total))
        {
            throw new InvalidOperationException("/proc/stat is not available for CPU sampling on this device.");
        }

        double cpuPercent = 0;
        if (_lastTotalCpuTicks > 0)
        {
            var totalDelta = total - _lastTotalCpuTicks;
            var idleDelta = idle - _lastIdleCpuTicks;
            if (totalDelta > 0)
            {
                cpuPercent = (1.0 - ((double)idleDelta / totalDelta)) * 100.0;
            }
        }

        _lastTotalCpuTicks = total;
        _lastIdleCpuTicks = idle;
        cpuPercent = Math.Clamp(cpuPercent, 0, 100);

        var totalProcessorTime = _currentProcess.TotalProcessorTime;

        var result = new CpuUsage
        {
            PercentageUsed = cpuPercent,
            TotalProcessorTimeMs = (long)totalProcessorTime.TotalMilliseconds,
            UserModeTimeMs = (long)_currentProcess.UserProcessorTime.TotalMilliseconds,
            KernelModeTimeMs = (long)_currentProcess.PrivilegedProcessorTime.TotalMilliseconds,
            ProcessorCount = Environment.ProcessorCount,
            CollectedAt = DateTimeOffset.UtcNow
        };

        return new ValueTask<CpuUsage>(result);
    }

    public ValueTask<MemorySnapshot> GetMemorySnapshotAsync(CancellationToken cancellationToken = default)
    {
        var totalMemory = GC.GetTotalMemory(false);
        var (totalSystem, availableSystem) = ReadMemInfo();

        var result = new MemorySnapshot
        {
            TotalSystemMemoryBytes = totalSystem,
            AvailableSystemMemoryBytes = availableSystem,
            ProcessWorkingSetBytes = _currentProcess.WorkingSet64,
            ProcessPrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
            ManagedHeapBytes = totalMemory,
            ProcessVirtualMemoryBytes = _currentProcess.VirtualMemorySize64,
            MemoryPressurePercentage = ComputeMemoryPressure(totalSystem, availableSystem),
            CollectedAt = DateTimeOffset.UtcNow
        };

        return new ValueTask<MemorySnapshot>(result);
    }

    public ValueTask<GcStats> GetGcStatsAsync(CancellationToken cancellationToken = default)
    {
        var mem = GC.GetGCMemoryInfo();
        double fragmentationPct = 0;
        if (mem.HeapSizeBytes > 0 && mem.FragmentedBytes >= 0)
        {
            fragmentationPct = (double)mem.FragmentedBytes / mem.HeapSizeBytes * 100.0;
        }

        var result = new GcStats
        {
            Gen0CollectionCount = GC.CollectionCount(0),
            Gen1CollectionCount = GC.CollectionCount(1),
            Gen2CollectionCount = GC.CollectionCount(2),
            TotalGcPauseMsPercentage = RuntimeCounters.Instance.TimeInGcPercent,
            HeapFragmentationPercentage = fragmentationPct,
            TotalAllocatedBytes = GC.GetTotalAllocatedBytes(),
            IsGcConcurrentEnabled = !GCSettings.IsServerGC,
            CollectedAt = DateTimeOffset.UtcNow
        };

        return new ValueTask<GcStats>(result);
    }

    public ValueTask<ThreadPoolStats> GetThreadPoolStatsAsync(CancellationToken cancellationToken = default)
    {
        ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);
        ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIoThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);

        var queuedItems = RuntimeCounters.Instance.ThreadPoolQueueLength;
        var completedItems = RuntimeCounters.Instance.ThreadPoolCompletedItemsCount;

        var result = new ThreadPoolStats
        {
            WorkerThreadCount = ThreadPool.ThreadCount,
            AvailableWorkerThreads = workerThreads,
            IoThreadCount = Environment.ProcessorCount,
            AvailableIoThreads = ioThreads,
            QueuedWorkItemCount = queuedItems,
            CompletedWorkItemCount = completedItems,
            MinWorkerThreads = minWorkerThreads,
            MaxWorkerThreads = maxWorkerThreads,
            CollectedAt = DateTimeOffset.UtcNow
        };

        return new ValueTask<ThreadPoolStats>(result);
    }

    private static bool TryReadCpuSample(out long idleTicks, out long totalTicks)
    {
        idleTicks = 0;
        totalTicks = 0;

        try
        {
            var line = File.ReadLines("/proc/stat").FirstOrDefault();
            if (line is null || !line.StartsWith("cpu "))
            {
                return false;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                return false;
            }

            long user = ParseLong(parts, 1);
            long nice = ParseLong(parts, 2);
            long system = ParseLong(parts, 3);
            long idle = ParseLong(parts, 4);
            long iowait = ParseLong(parts, 5);
            long irq = ParseLong(parts, 6);
            long softirq = ParseLong(parts, 7);
            long steal = ParseLong(parts, 8);
            long guest = ParseLong(parts, 9);
            long guestNice = ParseLong(parts, 10);

            idleTicks = idle + iowait;
            totalTicks = user + nice + system + idle + iowait + irq + softirq + steal + guest + guestNice;
            return totalTicks > 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static (long Total, long Available) ReadMemInfo()
    {
        long total = 0;
        long available = 0;

        try
        {
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemTotal:"))
                {
                    total = ParseMemInfoValue(line);
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    available = ParseMemInfoValue(line);
                }

                if (total > 0 && available > 0)
                {
                    break;
                }
            }
        }
        catch (IOException)
        {
            // ignored
        }
        catch (UnauthorizedAccessException)
        {
            // ignored
        }

        return (total, available);
    }

    private static int ComputeMemoryPressure(long totalBytes, long availableBytes)
    {
        if (totalBytes <= 0)
        {
            return 0;
        }

        var usedBytes = totalBytes - Math.Max(availableBytes, 0);
        return (int)Math.Clamp((double)usedBytes / totalBytes * 100.0, 0, 100);
    }

    private static long ParseMemInfoValue(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && long.TryParse(parts[1], out var valueKb))
        {
            return valueKb * 1024;
        }
        return 0;
    }

    private static long ParseLong(string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return 0;
        }
        return long.TryParse(parts[index], out var value) ? value : 0;
    }
}
