namespace DiagnosticsToolkit.Providers.Platforms;

using DiagnosticsToolkit.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DiagnosticsToolkit.Runtime;
using System.Runtime;

/// <summary>
/// Windows-specific runtime metrics provider using ETW and System.Diagnostics.Process.
/// </summary>
public partial class DefaultWindowsMetricsProvider : IRuntimeMetricsProvider
{
    private readonly Process _currentProcess;
    private DateTime _lastCpuSampleTime;
    private TimeSpan _lastTotalProcessorTime;

    public DefaultWindowsMetricsProvider()
    {
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuSampleTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        // Ensure runtime counters listener is initialized
        _ = RuntimeCounters.Instance;
    }

    /// <summary>
    /// Gets CPU usage by sampling process time.
    /// Uses a rolling calculation for accurate percentage reporting.
    /// </summary>
    public ValueTask<CpuUsage> GetCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalProcessorTime = _currentProcess.TotalProcessorTime;
        var cpuTimeDelta = totalProcessorTime - _lastTotalProcessorTime;
        var wallTimeDelta = now - _lastCpuSampleTime;

        _lastCpuSampleTime = now;
        _lastTotalProcessorTime = totalProcessorTime;

        // Normalize to 0-100 across all cores (divide by processor count)
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

    /// <summary>
    /// Gets memory metrics using Process and GC info.
    /// </summary>
    public ValueTask<MemorySnapshot> GetMemorySnapshotAsync(CancellationToken cancellationToken = default)
    {
        _currentProcess.Refresh();

        var gcInfo = GC.GetGCMemoryInfo();
        var totalMemory = GC.GetTotalMemory(false);

#if NET8_0_WINDOWS
        var status = new MEMORYSTATUSEX();
        status.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        if (!GlobalMemoryStatusEx(ref status))
        {
            throw new InvalidOperationException("GlobalMemoryStatusEx failed.");
        }

        var result = new MemorySnapshot
        {
            TotalSystemMemoryBytes = (long)status.ullTotalPhys,
            AvailableSystemMemoryBytes = (long)status.ullAvailPhys,
            ProcessWorkingSetBytes = _currentProcess.WorkingSet64,
            ProcessPrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
            ManagedHeapBytes = totalMemory,
            ProcessVirtualMemoryBytes = _currentProcess.VirtualMemorySize64,
            MemoryPressurePercentage = (int)status.dwMemoryLoad,
            CollectedAt = DateTimeOffset.UtcNow
        };

        return new ValueTask<MemorySnapshot>(result);
#else
        var result = new MemorySnapshot
        {
            TotalSystemMemoryBytes = 0,
            AvailableSystemMemoryBytes = 0,
            ProcessWorkingSetBytes = _currentProcess.WorkingSet64,
            ProcessPrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
            ManagedHeapBytes = totalMemory,
            ProcessVirtualMemoryBytes = _currentProcess.VirtualMemorySize64,
            MemoryPressurePercentage = 0,
            CollectedAt = DateTimeOffset.UtcNow
        };
        return new ValueTask<MemorySnapshot>(result);
#endif
    }

    /// <summary>
    /// Gets GC statistics from the runtime.
    /// </summary>
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

    /// <summary>
    /// Gets thread pool statistics.
    /// </summary>
    public ValueTask<ThreadPoolStats> GetThreadPoolStatsAsync(CancellationToken cancellationToken = default)
    {
        ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);
        ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIoThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);

        // Use runtime counters for queue length and completed items; works cross-process without ETW admin requirements.
        var queuedItems = RuntimeCounters.Instance.ThreadPoolQueueLength;
        var completedItems = RuntimeCounters.Instance.ThreadPoolCompletedItemsCount;

        var result = new ThreadPoolStats
        {
            WorkerThreadCount = ThreadPool.ThreadCount,
            AvailableWorkerThreads = workerThreads,
            IoThreadCount = Environment.ProcessorCount, // I/O thread count not directly exposed; approximate
            AvailableIoThreads = ioThreads,
            QueuedWorkItemCount = queuedItems,
            CompletedWorkItemCount = completedItems,
            MinWorkerThreads = minWorkerThreads,
            MaxWorkerThreads = maxWorkerThreads,
            CollectedAt = DateTimeOffset.UtcNow
        };

        return new ValueTask<ThreadPoolStats>(result);
    }

#if NET8_0_WINDOWS
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = false)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
#endif
}
