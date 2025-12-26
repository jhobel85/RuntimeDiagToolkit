using DiagnosticsToolkit.Models;
using Xunit;

namespace DiagnosticsToolkit.Tests;

public class MetricsModelTests
{
    [Fact]
    public void CpuUsage_CanBeCreated()
    {
        // Act
        var cpu = new CpuUsage
        {
            PercentageUsed = 45.5,
            ProcessorCount = 8,
            TotalProcessorTimeMs = 1000,
            UserModeTimeMs = 500,
            KernelModeTimeMs = 100
        };

        // Assert
        Assert.Equal(45.5, cpu.PercentageUsed);
        Assert.Equal(8, cpu.ProcessorCount);
    }

    [Fact]
    public void MemorySnapshot_CanBeCreated()
    {
        // Act
        var memory = new MemorySnapshot
        {
            TotalSystemMemoryBytes = 16_000_000_000,
            AvailableSystemMemoryBytes = 8_000_000_000,
            ProcessWorkingSetBytes = 500_000_000,
            ProcessPrivateMemoryBytes = 300_000_000,
            ManagedHeapBytes = 100_000_000,
            MemoryPressurePercentage = 50
        };

        // Assert
        Assert.Equal(16_000_000_000, memory.TotalSystemMemoryBytes);
        Assert.Equal(50, memory.MemoryPressurePercentage);
    }

    [Fact]
    public void GcStats_CanBeCreated()
    {
        // Act
        var gc = new GcStats
        {
            Gen0CollectionCount = 100,
            Gen1CollectionCount = 50,
            Gen2CollectionCount = 10,
            TotalGcPauseMsPercentage = 0.5,
            HeapFragmentationPercentage = 15.5,
            TotalAllocatedBytes = 1_000_000_000,
            IsGcConcurrentEnabled = true
        };

        // Assert
        Assert.Equal(100, gc.Gen0CollectionCount);
        Assert.Equal(0.5, gc.TotalGcPauseMsPercentage);
        Assert.True(gc.IsGcConcurrentEnabled);
    }

    [Fact]
    public void ThreadPoolStats_CanBeCreated()
    {
        // Act
        var threadPool = new ThreadPoolStats
        {
            WorkerThreadCount = 10,
            AvailableWorkerThreads = 5,
            IoThreadCount = 10,
            AvailableIoThreads = 8,
            QueuedWorkItemCount = 2,
            CompletedWorkItemCount = 1000,
            MinWorkerThreads = 1,
            MaxWorkerThreads = 32
        };

        // Assert
        Assert.Equal(10, threadPool.WorkerThreadCount);
        Assert.Equal(1000, threadPool.CompletedWorkItemCount);
    }

    [Fact]
    public void Metrics_HaveTimestamps()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var cpu = new CpuUsage { CollectedAt = now };
        var memory = new MemorySnapshot { CollectedAt = now };
        var gc = new GcStats { CollectedAt = now };
        var threadPool = new ThreadPoolStats { CollectedAt = now };

        // Assert
        Assert.Equal(now, cpu.CollectedAt);
        Assert.Equal(now, memory.CollectedAt);
        Assert.Equal(now, gc.CollectedAt);
        Assert.Equal(now, threadPool.CollectedAt);
    }
}
