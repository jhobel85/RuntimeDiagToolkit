namespace DiagnosticsToolkit.Models;

/// <summary>
/// Represents garbage collection statistics.
/*
GC Stats: Shows how often garbage collection occurs, memory pressure, and allocation patterns. 
Useful for:
- Detecting memory leaks
- Tuning GC settings for high-throughput apps
- Understanding pause times in real-time systems
*/
/// </summary>
public readonly record struct GcStats
{
    /// <summary>
    /// Total number of generation 0 collections.
    /// </summary>
    public long Gen0CollectionCount { get; init; }

    /// <summary>
    /// Total number of generation 1 collections.
    /// </summary>
    public long Gen1CollectionCount { get; init; }

    /// <summary>
    /// Total number of generation 2 collections.
    /// </summary>
    public long Gen2CollectionCount { get; init; }

    /// <summary>
    /// Total milliseconds spent in GC since process start.
    /// </summary>
    public double TotalGcPauseMsPercentage { get; init; }

    /// <summary>
    /// Heap fragmentation percentage (if available).
    /// </summary>
    public double HeapFragmentationPercentage { get; init; }

    /// <summary>
    /// Total bytes allocated since process start.
    /// </summary>
    public long TotalAllocatedBytes { get; init; }

    /// <summary>
    /// Is GC currently running (indication of GC pressure).
    /// </summary>
    public bool IsGcConcurrentEnabled { get; init; }

    /// <summary>
    /// Timestamp when the metric was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; }
}
