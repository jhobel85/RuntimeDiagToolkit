using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Runtime;

namespace DiagnosticsToolkit.Providers.Platforms;

/// <summary>
/// Linux-specific runtime metrics provider using /proc and runtime counters.
/// </summary>
public sealed class DefaultLinuxMetricsProvider : IRuntimeMetricsProvider
{
    private readonly Process _currentProcess;
    private long _lastIdleCpuTicks;
    private long _lastTotalCpuTicks;

    public DefaultLinuxMetricsProvider()
    {
        _currentProcess = Process.GetCurrentProcess();
        TryReadCpuSample(out _lastIdleCpuTicks, out _lastTotalCpuTicks);
        _ = RuntimeCounters.Instance; // start runtime counters listener eagerly
    }

    public ValueTask<CpuUsage> GetCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        if (!TryReadCpuSample(out var idle, out var total))
        {
            throw new InvalidOperationException("/proc/stat is not available for CPU sampling.");
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

        Span<byte> buffer = stackalloc byte[256];
        try
        {
            using var stream = File.OpenRead("/proc/stat");
            var read = stream.Read(buffer);
            if (read <= 0)
            {
                return false;
            }

            var span = buffer[..read];
            var newlineIndex = span.IndexOf((byte)'\n');
            if (newlineIndex > 0)
            {
                span = span[..newlineIndex];
            }

            return TryParseCpuLine(span, out idleTicks, out totalTicks);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryParseCpuLine(ReadOnlySpan<byte> line, out long idleTicks, out long totalTicks)
    {
        idleTicks = 0;
        totalTicks = 0;

        if (!line.StartsWith("cpu "u8))
        {
            return false;
        }

        line = line[4..];

        if (!TryReadNextLong(ref line, out var user, required: true) ||
            !TryReadNextLong(ref line, out var nice, required: true) ||
            !TryReadNextLong(ref line, out var system, required: true) ||
            !TryReadNextLong(ref line, out var idle, required: true) ||
            !TryReadNextLong(ref line, out var iowait))
        {
            return false;
        }

        TryReadNextLong(ref line, out var irq);
        TryReadNextLong(ref line, out var softirq);
        TryReadNextLong(ref line, out var steal);
        TryReadNextLong(ref line, out var guest);
        TryReadNextLong(ref line, out var guestNice);

        idleTicks = idle + iowait;
        totalTicks = user + nice + system + idle + iowait + irq + softirq + steal + guest + guestNice;
        return totalTicks > 0;
    }

    private static bool TryReadNextLong(ref ReadOnlySpan<byte> span, out long value, bool required = false)
    {
        value = 0;

        var index = 0;
        while (index < span.Length && span[index] == (byte)' ')
        {
            index++;
        }

        if (index >= span.Length)
        {
            span = ReadOnlySpan<byte>.Empty;
            return !required;
        }

        long result = 0;
        var start = index;
        for (; index < span.Length; index++)
        {
            var b = span[index];
            if (b < (byte)'0' || b > (byte)'9')
            {
                break;
            }

            result = (result * 10) + (b - (byte)'0');
        }

        if (index == start)
        {
            span = span[index..];
            return !required;
        }

        value = result;
        span = span[index..];
        return true;
    }

    private static (long Total, long Available) ReadMemInfo()
    {
        long total = 0;
        long available = 0;

        try
        {
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                var span = line.AsSpan();
                if (span.StartsWith("MemTotal:", StringComparison.Ordinal))
                {
                    total = ParseMemInfoValue(span);
                }
                else if (span.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    available = ParseMemInfoValue(span);
                }

                if (total > 0 && available > 0)
                {
                    break;
                }
            }
        }
        catch (IOException)
        {
            // ignored; fall back to zeros
        }
        catch (UnauthorizedAccessException)
        {
            // ignored; fall back to zeros
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
        var pressure = (int)Math.Clamp((double)usedBytes / totalBytes * 100.0, 0, 100);
        return pressure;
    }

    private static long ParseMemInfoValue(ReadOnlySpan<char> line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex < 0 || colonIndex + 1 >= line.Length)
        {
            return 0;
        }

        var remainder = line[(colonIndex + 1)..];

        var start = 0;
        while (start < remainder.Length && remainder[start] == ' ')
        {
            start++;
        }

        var end = start;
        while (end < remainder.Length && char.IsDigit(remainder[end]))
        {
            end++;
        }

        var valueSpan = remainder.Slice(start, end - start);
        if (valueSpan.IsEmpty)
        {
            return 0;
        }

        if (!long.TryParse(valueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueKb))
        {
            return 0;
        }

        return valueKb * 1024;
    }
}
