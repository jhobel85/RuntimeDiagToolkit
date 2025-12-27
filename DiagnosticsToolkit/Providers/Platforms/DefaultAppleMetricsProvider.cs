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
    private readonly object _sync = new();

    private TimeSpan _samplingInterval = TimeSpan.FromMilliseconds(250);
    private bool _isBackground;

    private DateTimeOffset _lastCpuSampleAt;
    private DateTimeOffset _lastMemSampleAt;
    private DateTimeOffset _lastGcSampleAt;
    private DateTimeOffset _lastTpSampleAt;

    private CpuUsage _lastCpu;
    private MemorySnapshot _lastMem;
    private GcStats _lastGc;
    private ThreadPoolStats _lastTp;

    public DefaultAppleMetricsProvider()
    {
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuSampleTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _ = RuntimeCounters.Instance; // ensure runtime counters listener is initialized
        var now = DateTimeOffset.UtcNow;
        _lastCpuSampleAt = now;
        _lastMemSampleAt = now;
        _lastGcSampleAt = now;
        _lastTpSampleAt = now;
    }

    public ValueTask<CpuUsage> GetCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_sync)
        {
            if (_isBackground || now - _lastCpuSampleAt < _samplingInterval)
            {
                return new ValueTask<CpuUsage>(_lastCpu);
            }

            var wallNow = DateTime.UtcNow;
            var totalProcessorTime = _currentProcess.TotalProcessorTime;
            var cpuTimeDelta = totalProcessorTime - _lastTotalProcessorTime;
            var wallTimeDelta = wallNow - _lastCpuSampleTime;

            _lastCpuSampleTime = wallNow;
            _lastTotalProcessorTime = totalProcessorTime;

            double cpuUsagePercent = 0;
            if (wallTimeDelta.TotalMilliseconds > 0)
            {
                cpuUsagePercent = (cpuTimeDelta.TotalMilliseconds / (wallTimeDelta.TotalMilliseconds * Environment.ProcessorCount)) * 100.0;
            }
            cpuUsagePercent = Math.Clamp(cpuUsagePercent, 0, 100);

            _lastCpu = new CpuUsage
            {
                PercentageUsed = cpuUsagePercent,
                TotalProcessorTimeMs = (long)totalProcessorTime.TotalMilliseconds,
                UserModeTimeMs = (long)_currentProcess.UserProcessorTime.TotalMilliseconds,
                KernelModeTimeMs = (long)_currentProcess.PrivilegedProcessorTime.TotalMilliseconds,
                ProcessorCount = Environment.ProcessorCount,
                CollectedAt = now
            };
            _lastCpuSampleAt = now;
            return new ValueTask<CpuUsage>(_lastCpu);
        }
    }

//Pressure is computed as used/total from VM stats; if sysctl totals are missing, it falls back to total derived from VM page counts.
    public ValueTask<MemorySnapshot> GetMemorySnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_sync)
        {
            if (_isBackground || now - _lastMemSampleAt < _samplingInterval)
            {
                return new ValueTask<MemorySnapshot>(_lastMem);
            }

            _currentProcess.Refresh();

            var gcTotal = GC.GetTotalMemory(false);

            long totalSystem = 0;
            long availableSystem = 0;

#if NET8_0 || NET8_0_MACCATALYST || NET8_0_IOS
        // Prefer native counters for total/available memory.
        if (!TryGetTotalMemoryBytes(out totalSystem))
        {
            totalSystem = 0;
        }

        if (!TryGetVmMemoryAvailability(out availableSystem, out var pageTotalBytes) && totalSystem == 0)
        {
            // If sysctl failed but VM stats returned a computed total, use that.
            totalSystem = pageTotalBytes;
        }
#endif

            // Compute a simple pressure percentage when totals are known.
            int pressurePct = 0;
            if (totalSystem > 0 && availableSystem >= 0)
            {
                var used = totalSystem - availableSystem;
                pressurePct = (int)Math.Clamp((double)used / totalSystem * 100.0, 0, 100);
            }

            _lastMem = new MemorySnapshot
            {
                TotalSystemMemoryBytes = totalSystem,
                AvailableSystemMemoryBytes = availableSystem,
                ProcessWorkingSetBytes = _currentProcess.WorkingSet64,
                ProcessPrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
                ManagedHeapBytes = gcTotal,
                ProcessVirtualMemoryBytes = _currentProcess.VirtualMemorySize64,
                MemoryPressurePercentage = pressurePct,
                CollectedAt = now
            };
            _lastMemSampleAt = now;
            return new ValueTask<MemorySnapshot>(_lastMem);
        }
    }

    public ValueTask<GcStats> GetGcStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_sync)
        {
            if (_isBackground || now - _lastGcSampleAt < _samplingInterval)
            {
                return new ValueTask<GcStats>(_lastGc);
            }

            var mem = GC.GetGCMemoryInfo();
            double fragmentationPct = 0;
            if (mem.HeapSizeBytes > 0 && mem.FragmentedBytes >= 0)
            {
                fragmentationPct = (double)mem.FragmentedBytes / mem.HeapSizeBytes * 100.0;
            }

            _lastGc = new GcStats
            {
                Gen0CollectionCount = GC.CollectionCount(0),
                Gen1CollectionCount = GC.CollectionCount(1),
                Gen2CollectionCount = GC.CollectionCount(2),
                TotalGcPauseMsPercentage = RuntimeCounters.Instance.TimeInGcPercent,
                HeapFragmentationPercentage = fragmentationPct,
                TotalAllocatedBytes = GC.GetTotalAllocatedBytes(),
                IsGcConcurrentEnabled = !GCSettings.IsServerGC,
                CollectedAt = now
            };
            _lastGcSampleAt = now;
            return new ValueTask<GcStats>(_lastGc);
        }
    }

    public ValueTask<ThreadPoolStats> GetThreadPoolStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_sync)
        {
            if (_isBackground || now - _lastTpSampleAt < _samplingInterval)
            {
                return new ValueTask<ThreadPoolStats>(_lastTp);
            }

            ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIoThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);

            var queuedItems = RuntimeCounters.Instance.ThreadPoolQueueLength;
            var completedItems = RuntimeCounters.Instance.ThreadPoolCompletedItemsCount;

            _lastTp = new ThreadPoolStats
            {
                WorkerThreadCount = ThreadPool.ThreadCount,
                AvailableWorkerThreads = workerThreads,
                IoThreadCount = Environment.ProcessorCount,
                AvailableIoThreads = ioThreads,
                QueuedWorkItemCount = queuedItems,
                CompletedWorkItemCount = completedItems,
                MinWorkerThreads = minWorkerThreads,
                MaxWorkerThreads = maxWorkerThreads,
                CollectedAt = now
            };
            _lastTpSampleAt = now;
            return new ValueTask<ThreadPoolStats>(_lastTp);
        }
    }

    // Sampling controls for mobile scenarios
    public void SetSamplingInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            interval = TimeSpan.FromMilliseconds(1);
        }
        lock (_sync)
        {
            _samplingInterval = interval;
        }
    }

    public void OnAppForegrounded()
    {
        lock (_sync)
        {
            _isBackground = false;
        }
    }

    public void OnAppBackgrounded()
    {
        lock (_sync)
        {
            _isBackground = true;
        }
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

    private static bool TryGetVmMemoryAvailability(out long availableBytes, out long computedTotalBytes)
    {
        availableBytes = 0;
        computedTotalBytes = 0;

#if NET8_0 || NET8_0_MACCATALYST || NET8_0_IOS
        try
        {
            var host = mach_host_self();
            if (host == IntPtr.Zero)
            {
                return false;
            }

            uint pageSize;
            if (host_page_size(host, out pageSize) != 0)
            {
                return false;
            }

            var stats = new vm_statistics64();
            int count = Marshal.SizeOf<vm_statistics64>() / sizeof(int);
            if (host_statistics64(host, HOST_VM_INFO64, ref stats, ref count) != 0)
            {
                return false;
            }

            var freePages = stats.free_count + stats.inactive_count + stats.speculative_count;
            var totalPages = stats.active_count + stats.inactive_count + stats.free_count + stats.wire_count + stats.speculative_count;

            availableBytes = checked((long)freePages * pageSize);
            computedTotalBytes = checked((long)totalPages * pageSize);
            return availableBytes >= 0 && computedTotalBytes > 0;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

#if NET8_0 || NET8_0_MACCATALYST || NET8_0_IOS
    [DllImport("libc", SetLastError = true)]
    private static extern int sysctlbyname(string name, out ulong oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);

    [DllImport("libSystem.dylib")]
    private static extern IntPtr mach_host_self();

    [DllImport("libSystem.dylib")]
    private static extern int host_statistics64(IntPtr host_priv, int flavor, ref vm_statistics64 stat, ref int count);

    [DllImport("libSystem.dylib")]
    private static extern int host_page_size(IntPtr host, out uint pageSize);

    private const int HOST_VM_INFO64 = 4;

    // Partial definition with fields we need for availability calculation.
    private struct vm_statistics64
    {
        public ulong free_count;
        public ulong active_count;
        public ulong inactive_count;
        public ulong wire_count;
        public ulong zero_fill_count;
        public ulong reactivations;
        public ulong pageins;
        public ulong pageouts;
        public ulong faults;
        public ulong cow_faults;
        public ulong lookups;
        public ulong hits;
        public ulong purgable_count;
        public ulong purges;
        public ulong speculative_count;
        public ulong decompressions;
        public ulong compressions;
        public ulong swapins;
        public ulong swapouts;
        public ulong compressor_page_count;
        public ulong throttled_count;
        public ulong external_page_count;
        public ulong internal_page_count;
        public ulong total_uncompressed_pages_in_compressor;
    }
#endif
}
