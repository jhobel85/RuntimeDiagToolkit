using DiagnosticsToolkit.Generators;
using System;
using System.Threading.Tasks;

namespace DiagnosticsToolkit.Generators.Sample;

/// <summary>
/// Example demonstrating runtime diagnostics collection using source generator.
/// The [RuntimeMetricsCollector] attribute generates methods to collect CPU, GC, Memory, and ThreadPool metrics.
/// </summary>
[RuntimeMetricsCollector]
public partial class RuntimeMetricsExample
{
    // Generated methods available:
    // - Task CollectMetricsAsync()
    // - CpuUsage GetLastCpuUsage()
    // - MemorySnapshot GetLastMemorySnapshot()
    // - GcStats GetLastGcStats()
    // - ThreadPoolStats GetLastThreadPoolStats()
    // - TimeSpan TimeSinceLastCollection()
    // - void StartAutoCollection(int intervalMs = 5000)
    // - void StopAutoCollection()
    // - Task<RuntimeMetricsSnapshot> GetCurrentSnapshotAsync()

    public static async Task DemonstrateUsageAsync()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  RUNTIME DIAGNOSTICS METRICS - SOURCE GENERATED");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Collect metrics once
        Console.WriteLine("Collecting runtime metrics...");
        await CollectMetricsAsync();
        Console.WriteLine();

        // Display CPU metrics
        var cpu = GetLastCpuUsage();
        Console.WriteLine($"🖥️  CPU Usage: {cpu.PercentageUsed:F2}% (across {cpu.ProcessorCount} cores)");
        Console.WriteLine($"   Total CPU Time: {cpu.TotalProcessorTimeMs} ms (User: {cpu.UserModeTimeMs} ms, Kernel: {cpu.KernelModeTimeMs} ms)");
        Console.WriteLine();

        // Display Memory metrics
        var memory = GetLastMemorySnapshot();
        Console.WriteLine($"💾 Memory:");
        Console.WriteLine($"   Working Set: {memory.ProcessWorkingSetBytes / 1024 / 1024:N0} MB");
        Console.WriteLine($"   Private Memory: {memory.ProcessPrivateMemoryBytes / 1024 / 1024:N0} MB");
        Console.WriteLine($"   Managed Heap: {memory.ManagedHeapBytes / 1024 / 1024:N0} MB");
        Console.WriteLine($"   Virtual Memory: {memory.ProcessVirtualMemoryBytes / 1024 / 1024:N0} MB");
        Console.WriteLine($"   System: {memory.TotalSystemMemoryBytes / 1024 / 1024:N0} MB total, {memory.AvailableSystemMemoryBytes / 1024 / 1024:N0} MB available, pressure {memory.MemoryPressurePercentage}%");
        Console.WriteLine();

        // Display GC metrics
        var gc = GetLastGcStats();
        Console.WriteLine($"🗑️  Garbage Collection:");
        Console.WriteLine($"   Gen 0: {gc.Gen0CollectionCount} collections");
        Console.WriteLine($"   Gen 1: {gc.Gen1CollectionCount} collections");
        Console.WriteLine($"   Gen 2: {gc.Gen2CollectionCount} collections");
        Console.WriteLine($"   Total Allocated: {gc.TotalAllocatedBytes / 1024 / 1024:N0} MB");
        Console.WriteLine($"   GC Pause: {gc.TotalGcPauseMsPercentage:F2}% of process time");
        Console.WriteLine($"   Heap Fragmentation: {gc.HeapFragmentationPercentage:F2}%");
        Console.WriteLine($"   Concurrent GC Enabled: {gc.IsGcConcurrentEnabled}");
        Console.WriteLine();

        // Display ThreadPool metrics
        var threadPool = GetLastThreadPoolStats();
        Console.WriteLine($"🧵 Thread Pool:");
        Console.WriteLine($"   Worker Threads: {threadPool.AvailableWorkerThreads}/{threadPool.WorkerThreadCount} (min {threadPool.MinWorkerThreads}, max {threadPool.MaxWorkerThreads})");
        Console.WriteLine($"   IO Threads: {threadPool.AvailableIoThreads}/{threadPool.IoThreadCount}");
        Console.WriteLine($"   Queued Work Items: {threadPool.QueuedWorkItemCount}");
        Console.WriteLine($"   Completed Work Items: {threadPool.CompletedWorkItemCount}");
        Console.WriteLine();

        Console.WriteLine($"⏱️  Time since collection: {TimeSinceLastCollection().TotalMilliseconds:F0} ms");
        Console.WriteLine();

        // Start automatic background collection
        Console.WriteLine("Starting automatic collection every 3 seconds...");
        StartAutoCollection(3000);

        await Task.Delay(10000); // Let it collect a few times

        StopAutoCollection();
        Console.WriteLine("Stopped automatic collection.");
        Console.WriteLine();

        // Get complete snapshot
        Console.WriteLine("Getting complete snapshot...");
        var snapshot = await GetCurrentSnapshotAsync();
        Console.WriteLine($"✅ Snapshot captured at {snapshot.Timestamp:HH:mm:ss.fff}");
        Console.WriteLine($"   CPU: {snapshot.CpuUsage.PercentageUsed:F2}%");
        Console.WriteLine($"   Memory: {snapshot.Memory.ProcessWorkingSetBytes / 1024 / 1024:N0} MB");
        Console.WriteLine($"   GC Collections (Gen2): {snapshot.Gc.Gen2CollectionCount}");
        Console.WriteLine($"   ThreadPool Available: {snapshot.ThreadPool.AvailableWorkerThreads}/{snapshot.ThreadPool.WorkerThreadCount}");
    }
}
