using DiagnosticsToolkit.Models;
using DiagnosticsToolkit.Providers;
using Xunit;

namespace DiagnosticsToolkit.Tests;

public class RuntimeMetricsProviderTests
{
    private readonly IRuntimeMetricsProvider _provider = RuntimeMetricsProviderFactory.Create();

    [Fact]
    public async Task GetCpuUsageAsync_ReturnsValidMetrics()
    {
        var cpu = await _provider.GetCpuUsageAsync();

        Assert.True(cpu.PercentageUsed >= 0);
        Assert.True(cpu.PercentageUsed <= 100);
        Assert.True(cpu.ProcessorCount > 0);
        Assert.True(cpu.TotalProcessorTimeMs >= 0);
    }

    [Fact]
    public async Task GetMemorySnapshotAsync_ReturnsValidMetrics()
    {
        var memory = await _provider.GetMemorySnapshotAsync();

        Assert.True(memory.TotalSystemMemoryBytes >= 0);
        Assert.True(memory.AvailableSystemMemoryBytes >= 0);
        Assert.True(memory.ProcessWorkingSetBytes >= 0);
        Assert.True(memory.ProcessPrivateMemoryBytes >= 0);
        Assert.True(memory.ManagedHeapBytes >= 0);
        Assert.NotEqual(default(DateTimeOffset), memory.CollectedAt);
    }

    [Fact]
    public async Task GetGcStatsAsync_ReturnsValidMetrics()
    {
        var gc = await _provider.GetGcStatsAsync();

        Assert.True(gc.Gen0CollectionCount >= 0);
        Assert.True(gc.Gen1CollectionCount >= 0);
        Assert.True(gc.Gen2CollectionCount >= 0);
        Assert.True(gc.TotalGcPauseMsPercentage >= 0);
        Assert.True(gc.HeapFragmentationPercentage >= 0);
        Assert.True(gc.TotalAllocatedBytes >= 0);
        Assert.NotEqual(default(DateTimeOffset), gc.CollectedAt);
    }

    [Fact]
    public async Task GetThreadPoolStatsAsync_ReturnsValidMetrics()
    {
        var threadPool = await _provider.GetThreadPoolStatsAsync();

        Assert.True(threadPool.WorkerThreadCount > 0);
        Assert.True(threadPool.AvailableWorkerThreads >= 0);
        Assert.True(threadPool.IoThreadCount >= 0);
        Assert.True(threadPool.AvailableIoThreads >= 0);
        Assert.True(threadPool.QueuedWorkItemCount >= 0);
        Assert.True(threadPool.CompletedWorkItemCount >= 0);
        Assert.True(threadPool.MinWorkerThreads > 0);
        Assert.True(threadPool.MaxWorkerThreads >= threadPool.MinWorkerThreads);
        Assert.NotEqual(default(DateTimeOffset), threadPool.CollectedAt);
    }

    [Fact]
    public async Task MultipleCalls_ProduceConsistentResults()
    {
        var cpu1 = await _provider.GetCpuUsageAsync();
        await Task.Delay(100);
        var cpu2 = await _provider.GetCpuUsageAsync();

        // Processor count should remain the same across calls
        Assert.Equal(cpu2.ProcessorCount, cpu1.ProcessorCount);
    }

    [Fact]
    public async Task ConcurrentMetricsCollection_DoesNotThrow()
    {
        async Task CollectMetricsOnce()
        {
            _ = await _provider.GetCpuUsageAsync();
            _ = await _provider.GetMemorySnapshotAsync();
            _ = await _provider.GetGcStatsAsync();
            _ = await _provider.GetThreadPoolStatsAsync();
        }

        var tasks = Enumerable.Range(0, 10).Select(_ => CollectMetricsOnce()).ToArray();
        await Task.WhenAll(tasks);
    }
}
