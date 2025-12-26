using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Runtime;

namespace DiagnosticsToolkit.Providers.Platforms;

/// <summary>
/// macOS/iOS/Mac Catalyst metrics provider using process sampling and runtime counters.
/// Falls back gracefully when OS-specific memory details are unavailable.
/// </summary>
public sealed class DefaultAppleMetricsProvider : IRuntimeMetricsProvider
{
    private readonly Process _currentProcess;
    private DateTime _lastCpuSampleTime;
    private TimeSpan _lastTotalProcessorTime;

    public DefaultAppleMetricsProvider()
    {
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuSampleTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _ = RuntimeCounters.Instance; // ensure runtime counters listener is initialized
    }

    public ValueTask<CpuUsage> GetCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalProcessorTime = _currentProcess.TotalProcessorTime;
        var cpuTimeDelta = totalProcessorTime - _lastTotalProcessorTime;
        var wallTimeDelta = now - _lastCpuSampleTime;

        _lastCpuSampleTime = now;
        _lastTotalProcessorTime = totalProcessorTime;

        double cpuUsagePercent = 0;
        if (wallTimeDelta.TotalMilliseconds > 0)
        {
            cpuUsagePercent = (cpuTimeDelta.TotalMilliseconds / (wallTimeDelta.TotalMilliseconds * Environment.ProcessorCount)) * 100.0;
        }
        cpuUsagePercent = Math.Clamp(cpuUsagePercent, 0, 100);

        var result = new CpuUsage
        {
            PercentageUsed = cpuUsagePercent,
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
        _currentProcess.Refresh();

        var gcTotal = GC.GetTotalMemory(false);
        var totalSystem = TryGetTotalMemoryBytes(out var memBytes) ? memBytes : 0;

        var result = new MemorySnapshot
        {
            TotalSystemMemoryBytes = totalSystem,
            AvailableSystemMemoryBytes = 0, // Apple platforms do not expose a simple free memory counter without heavier interop
            ProcessWorkingSetBytes = _currentProcess.WorkingSet64,
            ProcessPrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
            ManagedHeapBytes = gcTotal,
            ProcessVirtualMemoryBytes = _currentProcess.VirtualMemorySize64,
            MemoryPressurePercentage = 0,
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

    private static bool TryGetTotalMemoryBytes(out long totalBytes)
    {
        totalBytes = 0;

        // hw.memsize is available on macOS/iOS; returns bytes.
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
        {
            try
            {
                nuint len = (nuint)Marshal.SizeOf<ulong>();
                if (sysctlbyname("hw.memsize", out ulong value, ref len, IntPtr.Zero, 0) == 0)
                {
                    totalBytes = unchecked((long)value);
                    return totalBytes > 0;
                }
            }
            catch
            {
                // ignore; fall through to false
            }
        }

        return false;
    }

#if NET8_0 || NET8_0_MACCATALYST || NET8_0_IOS
    [DllImport("libc", SetLastError = true)]
    private static extern int sysctlbyname(string name, out ulong oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);
#endif
}
